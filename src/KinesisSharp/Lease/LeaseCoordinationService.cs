using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis.Model;
using KinesisSharp.Common;
using KinesisSharp.Configuration;
using KinesisSharp.Lease.Registry;
using KinesisSharp.Shards;
using Microsoft.Extensions.Options;

namespace KinesisSharp.Lease
{
    public class LeaseMatchingResult
    {
        public LeaseMatchingResult(IReadOnlyCollection<Lease> leasesToBeCreated,
            IReadOnlyCollection<Lease> leasesToBeDeleted)
        {
            LeasesToBeCreated = leasesToBeCreated;
            LeasesToBeDeleted = leasesToBeDeleted;
        }

        public IReadOnlyCollection<Lease> LeasesToBeCreated { get; }

        public IReadOnlyCollection<Lease> LeasesToBeDeleted { get; }
    }

    public interface ILeaseCoordinationService
    {
        Task<LeaseMatchingResult> ResolveLeasesForShards(CancellationToken token = default);
    }

    public class LeaseCoordinationService : ILeaseCoordinationService
    {
        private readonly IDiscoverShards discoverShards;
        private readonly ILeaseRegistryQuery leaseQuery;
        private readonly IOptions<ApplicationConfiguration> streamConfiguration;

        public LeaseCoordinationService(IOptions<ApplicationConfiguration> streamConfiguration,
            ILeaseRegistryQuery leaseQuery, IDiscoverShards discoverShards)
        {
            this.streamConfiguration = streamConfiguration;
            this.leaseQuery = leaseQuery;
            this.discoverShards = discoverShards;
        }

        public async Task<LeaseMatchingResult> ResolveLeasesForShards(CancellationToken token = default)
        {
            var shardMap =
                new ShardMap(await discoverShards.GetShardsAsync(streamConfiguration.Value.StreamArn, token)
                    .ConfigureAwait(false));

            var leaseMap =
                new LeaseMap(await leaseQuery.GetAllLeasesAsync(streamConfiguration.Value.ApplicationName, token)
                    .ConfigureAwait(false));

            var openShards = GetOpenShards(shardMap).ToList();

            var requestedPosition = new InitialPosition(streamConfiguration.Value.Position,
                streamConfiguration.Value.TimeStamp);

            var newShardsForLeases = FindNewLeases(openShards, requestedPosition, shardMap, leaseMap);

            var leasesToDelete = FindLeasesToBeDeleted(openShards, requestedPosition, shardMap, leaseMap);

            var newLeases = newShardsForLeases.Select(s => new Lease
            {
                ShardId = s.ShardId,
                Checkpoint = ShardPosition.TrimHorizon
            }).ToList();

            return new LeaseMatchingResult(new ReadOnlyCollection<Lease>(newLeases),
                new ReadOnlyCollection<Lease>(leasesToDelete));
        }

        private IList<Lease> FindLeasesToBeDeleted(IList<Shard> openShards, InitialPosition requestedPosition,
            ShardMap shardMap, LeaseMap leaseMap)
        {
            return openShards.SelectMany(
                openShard => FindObsolete(requestedPosition, shardMap, leaseMap, openShard)
            ).ToList();
        }

        private static List<Shard> FindNewLeases(IList<Shard> openShards, InitialPosition requestedPosition,
            ShardMap shardMap, LeaseMap leaseMap)
        {
            return openShards
                .SelectMany(s =>
                    FindDeepestReachable(requestedPosition, shardMap, s)
                        .Where(x => (NoParents(x) || AllParentsClosed(x, leaseMap)) &&
                                    !leaseMap.ContainsKey(x.ShardId)))
                .Distinct(new ShardEqualityComparer())
                .ToList();
        }

        private static bool NoParents(Shard x)
        {
            return x.ParentShardId == null && x.AdjacentParentShardId == null;
        }

        private static IEnumerable<Shard> GetOpenShards(ShardMap map)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            return map.Values.Where(IsOpen);
        }

        private static bool IsOpen(Shard s)
        {
            return s.SequenceNumberRange?.EndingSequenceNumber == null;
        }


        private static bool AllParentsClosed(Shard shard, LeaseMap leaseMap)
        {
            if (shard.ParentShardId != null)
            {
                if (!CheckParentEnded(shard.ParentShardId, leaseMap))
                {
                    return false;
                }
            }

            if (shard.AdjacentParentShardId != null)
            {
                if (!CheckParentEnded(shard.AdjacentParentShardId, leaseMap))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CheckParentEnded(string parentId, LeaseMap leaseMap)
        {
            if (!leaseMap.ContainsKey(parentId))
            {
                return false;
            }

            var lease = leaseMap[parentId];
            if (!lease.Checkpoint.IsEnded)
            {
                return false;
            }

            return true;
        }

        private static Lease TryLookup(string parentShardId, LeaseMap leaseMap)
        {
            return parentShardId != null && leaseMap.ContainsKey(parentShardId) ? leaseMap[parentShardId] : null;
        }

        private static IEnumerable<(Shard Shard, Lease Lease)> IterateAncestorsFromOpenShard(Shard openShard,
            ShardMap shardMap, LeaseMap leaseMap)
        {
            if (!IsOpen(openShard))
            {
                throw new InvalidOperationException("Shard isn't open");
            }

            foreach (var ancestor in shardMap.SelectAllAncestors(openShard))
            {
                yield return (Shard: ancestor, Lease: leaseMap[ancestor.ShardId]);
            }

            yield return (Shard: openShard, Lease: leaseMap[openShard.ShardId]);
        }


        private static IEnumerable<Lease> FindObsolete(InitialPosition requestedPosition, ShardMap map,
            LeaseMap leaseMap,
            Shard currentOpenShard)
        {
            switch (requestedPosition.Type)
            {
                case InitialPositionType.TrimHorizon:
                    leaseMap.TryGetValue(currentOpenShard.ShardId, out var lease);
                    return FindDeepestObsoleteRecursive(false, currentOpenShard, requestedPosition, map, leaseMap);

                case InitialPositionType.Latest:
                case InitialPositionType.AtTimeStamp:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IEnumerable<Lease> FindDeepestObsoleteRecursive(bool ancestorStarted, Shard shard,
            InitialPosition requestedPosition,
            ShardMap map, LeaseMap leaseMap)
        {
            if (leaseMap.TryGetValue(shard.ShardId, out var lease))
            {
                if (lease.Checkpoint.IsEnded && ancestorStarted)
                {
                    yield return lease;
                }
            }

            if (shard.ParentShardId != null && map.ContainsKey(shard.ParentShardId))
            {
                foreach (var s in FindDeepestObsoleteRecursive(lease?.Checkpoint?.IsStarted ?? false,
                    map[shard.ParentShardId], requestedPosition, map, leaseMap))
                {
                    yield return s;
                }
            }

            if (shard.AdjacentParentShardId != null && map.ContainsKey(shard.AdjacentParentShardId))
            {
                foreach (var s in FindDeepestObsoleteRecursive(lease?.Checkpoint?.IsStarted ?? false,
                    map[shard.AdjacentParentShardId], requestedPosition, map,
                    leaseMap))
                {
                    yield return s;
                }
            }
        }

        private static IEnumerable<Shard> FindDeepestReachable(InitialPosition requestedPosition, ShardMap map,
            Shard currentOpenShard)
        {
            switch (requestedPosition.Type)
            {
                case InitialPositionType.TrimHorizon:
                    return map.SelectAllAncestors(currentOpenShard).Concat(new[] {currentOpenShard});
                case InitialPositionType.AtTimeStamp:
                case InitialPositionType.Latest:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private class ShardEqualityComparer : EqualityComparer<Shard>
        {
            public override bool Equals(Shard x, Shard y)
            {
                return Equals(x?.ShardId, y?.ShardId);
            }

            public override int GetHashCode(Shard obj)
            {
                return obj?.ShardId.GetHashCode() ?? 0;
            }
        }
    }
}