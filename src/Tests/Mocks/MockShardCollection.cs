using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amazon.Kinesis.Model;

namespace Tests.Mocks
{
    public class MockShardCollection : Collection<Shard>
    {
        private readonly Dictionary<string, Shard> innerDictionary = new Dictionary<string, Shard>();

        protected override void RemoveItem(int index)
        {
            var item = Items[index];
            innerDictionary.Remove(item.ShardId);
            base.RemoveItem(index);
        }

        public Shard Create(string id, long fromHash, long? toHash)
        {
            var shard = Shards.Create(id, fromHash, toHash);
            innerDictionary[shard.ShardId] = shard;
            Add(shard);
            return shard;
        }

        public Shard Merge(string id, string leftShard, string rightShard, long atPosition)
        {
            if (!innerDictionary.ContainsKey(leftShard))
            {
                throw new ArgumentException($"{leftShard} isn't added to the collection", nameof(leftShard));
            }

            var left = innerDictionary[leftShard];

            if (!innerDictionary.ContainsKey(rightShard))
            {
                throw new ArgumentException($"{rightShard} isn't added to the collection", nameof(rightShard));
            }

            var right = innerDictionary[rightShard];
            var merge = Shards.Merge(id, left, right, atPosition);
            innerDictionary[id] = merge;
            Add(merge);
            return merge;
        }

        public (Shard First, Shard Second) Split(string sourceShard, string position, string firstShardId,
            string secondShardId, long atHash)
        {
            if (!innerDictionary.ContainsKey(sourceShard))
            {
                throw new ArgumentException($"{sourceShard} isn't added to the collection", nameof(sourceShard));
            }

            var source = innerDictionary[sourceShard];
            var (first, second) = Shards.Split(source, position, firstShardId, secondShardId, atHash);
            innerDictionary[first.ShardId] = first;
            innerDictionary[second.ShardId] = second;
            Add(first);
            Add(second);
            return (first, second);
        }
    }
}