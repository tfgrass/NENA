using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NENA
{
    /// <summary>
    /// A thread-safe queue that prevents duplicate items.
    /// Wraps a BlockingCollection internally and tracks membership
    /// so each file path is only enqueued once until it’s actually taken.
    /// </summary>
    public sealed class UniqueFileQueue : IDisposable
    {
        private readonly BlockingCollection<string> _collection;
        private readonly ConcurrentDictionary<string, bool> _inQueue;

        public UniqueFileQueue()
        {
            _collection = new BlockingCollection<string>();
            _inQueue = new ConcurrentDictionary<string, bool>();
        }

        /// <summary>
        /// Attempts to enqueue a file path if it's not already present.
        /// Returns true if the file was actually added, false if it was skipped.
        /// </summary>
        public bool TryAdd(string filePath)
        {
            // If filePath isn't in the dictionary, we add it (and set it to `true`).
            // If it was already in the dictionary, TryAdd returns false, meaning skip.
            if (_inQueue.TryAdd(filePath, true))
            {
                _collection.Add(filePath);
                return true;
            }

            // Already enqueued
            return false;
        }

        /// <summary>
        /// Complete adding so consumers know no more items will arrive.
        /// </summary>
        public void CompleteAdding()
        {
            _collection.CompleteAdding();
        }

        /// <summary>
        /// GetConsumingEnumerable for consumers like a queue processor.
        /// Each time we yield an item, we remove it from the dictionary
        /// so it can be re-enqueued in the future if needed.
        /// </summary>
        public IEnumerable<string> GetConsumingEnumerable()
        {
            foreach (var item in _collection.GetConsumingEnumerable())
            {
                _inQueue.TryRemove(item, out _);
                yield return item;
            }
        }

        public void Dispose()
        {
            _collection.Dispose();
        }
    }
}
