using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpcToolkit.Middleware
{
    /// <summary>
    /// Storage contract for rate-limit counters. Implementations used by
    /// multiple processes must increment atomically.
    /// </summary>
    public interface IRateLimitStore
    {
        /// <summary>
        /// Record a hit and return the number of hits inside the current window.
        /// </summary>
        Task<int> IncrementAsync(string key, TimeSpan window, DateTime now);
    }

    /// <summary>
    /// In-process sliding-window store. Use a shared implementation for
    /// multi-process or multi-instance deployments.
    /// </summary>
    public sealed class MemoryRateLimitStore : IRateLimitStore
    {
        private readonly ConcurrentDictionary<string, RateLimitEntry> _requests = new();

        /// <inheritdoc />
        public Task<int> IncrementAsync(string key, TimeSpan window, DateTime now)
        {
            var entry = _requests.GetOrAdd(key, _ => new RateLimitEntry());

            lock (entry)
            {
                entry.Requests.RemoveAll(timestamp => now - timestamp > window);
                entry.Requests.Add(now);
                return Task.FromResult(entry.Requests.Count);
            }
        }

        private sealed class RateLimitEntry
        {
            public List<DateTime> Requests { get; } = new();
        }
    }
}
