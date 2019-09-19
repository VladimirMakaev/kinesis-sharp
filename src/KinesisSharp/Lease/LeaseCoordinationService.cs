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
                new ShardMap(await discoverShards.GetShardsAsync(streamConfiguration.Value.StreamArn, token).ConfigureAwait(false));

            var leaseMap =
                new LeaseMap(await leaseQuery.GetAllLeasesAsync(streamConfiguration.Value.ApplicationName, token).ConfigureAwait(false));

            var openShards = GetOpenShards(shardMap);

            var requestedPosition = new InitialPosition(streamConfiguration.Value.Position,
                streamConfiguration.Value.TimeStamp);

            var newShardsForLeases = openShards
                .SelectMany(s =>
                    FindDeepestReachable(requestedPosition, shardMap, s).Where(x =>
                        x.ParentShardId == null && x.AdjacentParentShardId == null))
                .Distinct(new ShardEqualityComparer())
                .ToList();

            var newLeases = newShardsForLeases.Select(s => new Lease
            {
                ShardId = s.ShardId,
                Checkpoint = ShardPosition.TrimHorizon
            }).ToList();

            return new LeaseMatchingResult(new ReadOnlyCollection<Lease>(newLeases), null);
        }

        private static IEnumerable<Shard> GetOpenShards(ShardMap map)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            return map.Values.Where(s => s.SequenceNumberRange?.EndingSequenceNumber == null);
        }

        private static IEnumerable<Shard> FindDeepestReachable(InitialPosition requestedPosition, ShardMap map,
            Shard shard)
        {
            switch (requestedPosition.Type)
            {
                case InitialPositionType.TrimHorizon:
                    return map.SelectAllAncestors(shard).Concat(new[] {shard});
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