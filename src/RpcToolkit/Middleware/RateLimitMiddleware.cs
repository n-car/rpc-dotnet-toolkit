using System;
using System.Security.Claims;
using System.Security.Principal;
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
        private readonly IRateLimitStore _store;

        /// <summary>
        /// Create rate limiter
        /// </summary>
        /// <param name="maxRequests">Maximum requests per window</param>
        /// <param name="windowSeconds">Time window in seconds</param>
        /// <param name="keySelector">How to identify clients: "ip", "user", "global"</param>
        /// <param name="store">Optional shared store. Defaults to in-process memory.</param>
        public RateLimitMiddleware(
            int maxRequests,
            int windowSeconds,
            string keySelector = "ip",
            IRateLimitStore? store = null)
        {
            if (maxRequests <= 0)
                throw new ArgumentException("Max requests must be positive", nameof(maxRequests));
            if (windowSeconds <= 0)
                throw new ArgumentException("Window must be positive", nameof(windowSeconds));

            _maxRequests = maxRequests;
            _window = TimeSpan.FromSeconds(windowSeconds);
            _keySelector = keySelector.ToLowerInvariant();
            _store = store ?? new MemoryRateLimitStore();
        }

        /// <summary>
        /// Validates rate limit before processing the request
        /// </summary>
        /// <param name="request">The RPC request</param>
        /// <param name="context">Request context used for rate limit key extraction</param>
        /// <exception cref="RateLimitExceededException">Thrown when rate limit is exceeded</exception>
        public async Task BeforeAsync(RpcRequest request, object? context)
        {
            var key = GetKey(context);
            var now = DateTime.UtcNow;
            var requestCount = await _store.IncrementAsync(key, _window, now);

            if (requestCount > _maxRequests)
            {
                throw new RateLimitExceededException(
                    $"Rate limit exceeded: {_maxRequests} requests per {_window.TotalSeconds}s");
            }
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
            if (context is RpcRequestContext rpcContext)
                return rpcContext.RemoteIp ?? "unknown";

            return GetPropertyValue(context, "RemoteIp")
                ?? GetPropertyValue(context, "ClientIp")
                ?? "unknown";
        }

        private string GetUserId(object? context)
        {
            if (context is RpcRequestContext rpcContext)
                return GetIdentityKey(rpcContext.Principal ?? rpcContext.User) ?? "anonymous";

            var user = context?.GetType().GetProperty("User")?.GetValue(context);
            return GetPropertyValue(context, "UserId")
                ?? GetIdentityKey(user)
                ?? "anonymous";
        }

        private static string? GetIdentityKey(object? user)
        {
            if (user is ClaimsPrincipal claimsPrincipal)
                return claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? claimsPrincipal.Identity?.Name;

            if (user is IPrincipal principal)
                return principal.Identity?.Name;

            return GetPropertyValue(user, "UserId")
                ?? GetPropertyValue(user, "Id")
                ?? user?.ToString();
        }

        private static string? GetPropertyValue(object? value, string propertyName) =>
            value?.GetType().GetProperty(propertyName)?.GetValue(value)?.ToString();
    }
}
