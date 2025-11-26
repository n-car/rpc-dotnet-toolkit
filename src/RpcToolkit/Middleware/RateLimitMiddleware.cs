using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using RpcToolkit.Exceptions;

namespace RpcToolkit.Middleware
{
    /// <summary>
    /// Rate limiting middleware
    /// </summary>
    public class RateLimitMiddleware : IMiddleware
    {
        private readonly int _maxRequests;
        private readonly TimeSpan _window;
        private readonly string _keySelector;
        private readonly ConcurrentDictionary<string, RateLimitEntry> _requests = new();

        /// <summary>
        /// Create rate limiter
        /// </summary>
        /// <param name="maxRequests">Maximum requests per window</param>
        /// <param name="windowSeconds">Time window in seconds</param>
        /// <param name="keySelector">How to identify clients: "ip", "user", "global"</param>
        public RateLimitMiddleware(int maxRequests, int windowSeconds, string keySelector = "ip")
        {
            if (maxRequests <= 0)
                throw new ArgumentException("Max requests must be positive", nameof(maxRequests));
            if (windowSeconds <= 0)
                throw new ArgumentException("Window must be positive", nameof(windowSeconds));

            _maxRequests = maxRequests;
            _window = TimeSpan.FromSeconds(windowSeconds);
            _keySelector = keySelector.ToLowerInvariant();
        }

        /// <summary>
        /// Validates rate limit before processing the request
        /// </summary>
        /// <param name="request">The RPC request</param>
        /// <param name="context">Request context used for rate limit key extraction</param>
        /// <exception cref="RateLimitExceededException">Thrown when rate limit is exceeded</exception>
        public Task BeforeAsync(RpcRequest request, object? context)
        {
            var key = GetKey(context);
            var now = DateTime.UtcNow;

            var entry = _requests.GetOrAdd(key, _ => new RateLimitEntry());

            lock (entry)
            {
                // Clean old requests
                entry.Requests.RemoveAll(t => now - t > _window);

                // Check limit
                if (entry.Requests.Count >= _maxRequests)
                {
                    throw new RateLimitExceededException(
                        $"Rate limit exceeded: {_maxRequests} requests per {_window.TotalSeconds}s");
                }

                // Add current request
                entry.Requests.Add(now);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Executes after request processing (no-op for rate limit middleware)
        /// </summary>
        /// <param name="request">The RPC request</param>
        /// <param name="result">The request result</param>
        /// <param name="context">Request context</param>
        public Task AfterAsync(RpcRequest request, object? result, object? context)
        {
            return Task.CompletedTask;
        }

        private string GetKey(object? context)
        {
            return _keySelector switch
            {
                "ip" => GetClientIp(context),
                "user" => GetUserId(context),
                "global" => "global",
                _ => "unknown"
            };
        }

        private string GetClientIp(object? context)
        {
            // In real implementation, extract from HttpContext
            // For now, use a placeholder
            return context?.GetType().GetProperty("ClientIp")?.GetValue(context)?.ToString() ?? "unknown";
        }

        private string GetUserId(object? context)
        {
            // In real implementation, extract authenticated user
            return context?.GetType().GetProperty("UserId")?.GetValue(context)?.ToString() ?? "anonymous";
        }

        private class RateLimitEntry
        {
            public System.Collections.Generic.List<DateTime> Requests { get; } = new();
        }
    }
}
