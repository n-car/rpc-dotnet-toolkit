using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RpcToolkit.Exceptions;

namespace RpcToolkit.Middleware
{
    /// <summary>
    /// Options for configuring the method whitelist middleware.
    /// </summary>
    public class MethodWhitelistOptions
    {
        /// <summary>
        /// List of allowed method names. If empty, all methods are allowed.
        /// Supports wildcard patterns with * (e.g., "user.*", "*.list").
        /// </summary>
        public List<string> AllowedMethods { get; set; } = new List<string>();

        /// <summary>
        /// List of explicitly denied method names. Takes precedence over allowed methods.
        /// Supports wildcard patterns with * (e.g., "admin.*", "*.delete").
        /// </summary>
        public List<string> DeniedMethods { get; set; } = new List<string>();

        /// <summary>
        /// Case-sensitive method name matching.
        /// Default is false (case-insensitive).
        /// </summary>
        public bool CaseSensitive { get; set; } = false;

        /// <summary>
        /// Custom error message when method is not allowed.
        /// Default is "Method not allowed: {method}".
        /// </summary>
        public string ErrorMessage { get; set; } = "Method not allowed: {method}";

        /// <summary>
        /// Error code to return when method is not allowed.
        /// Default is -32601 (Method not found).
        /// </summary>
        public int ErrorCode { get; set; } = -32601;
    }

    /// <summary>
    /// Middleware that restricts which RPC methods can be called.
    /// Similar to Express.js method filtering middleware.
    /// </summary>
    public class MethodWhitelistMiddleware : IMiddleware
    {
        private readonly MethodWhitelistOptions _options;
        private readonly StringComparison _comparison;

        /// <summary>
        /// Creates a new method whitelist middleware with specified allowed methods.
        /// </summary>
        /// <param name="allowedMethods">List of allowed method names.</param>
        public MethodWhitelistMiddleware(params string[] allowedMethods)
            : this(new MethodWhitelistOptions
            {
                AllowedMethods = allowedMethods.ToList()
            })
        {
        }

        /// <summary>
        /// Creates a new method whitelist middleware with specified options.
        /// </summary>
        /// <param name="options">Configuration options.</param>
        public MethodWhitelistMiddleware(MethodWhitelistOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _comparison = _options.CaseSensitive 
                ? StringComparison.Ordinal 
                : StringComparison.OrdinalIgnoreCase;
        }

        /// <summary>
        /// Execute before the RPC method is called.
        /// </summary>
        public Task BeforeAsync(RpcRequest request, object? context)
        {
            var method = request.Method;

            // Check if method is explicitly denied
            if (IsMethodDenied(method))
            {
                throw new MethodNotFoundException(_options.ErrorMessage.Replace("{method}", method));
            }

            // If whitelist is specified, check if method is allowed
            if (_options.AllowedMethods.Count > 0 && !IsMethodAllowed(method))
            {
                throw new MethodNotFoundException(_options.ErrorMessage.Replace("{method}", method));
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Execute after the RPC method is called.
        /// </summary>
        public Task AfterAsync(RpcRequest request, object? result, object? context)
        {
            return Task.CompletedTask;
        }

        private bool IsMethodAllowed(string method)
        {
            return _options.AllowedMethods.Any(pattern => MatchesPattern(method, pattern));
        }

        private bool IsMethodDenied(string method)
        {
            return _options.DeniedMethods.Any(pattern => MatchesPattern(method, pattern));
        }

        private bool MatchesPattern(string method, string pattern)
        {
            // Handle wildcard patterns
            if (pattern.Contains("*"))
            {
                // Convert wildcard pattern to regex-like matching
                var parts = pattern.Split('*');
                
                // Pattern with * at the start (e.g., "*.list")
                if (pattern.StartsWith("*"))
                {
                    return method.EndsWith(parts[1], _comparison);
                }
                
                // Pattern with * at the end (e.g., "user.*")
                if (pattern.EndsWith("*"))
                {
                    return method.StartsWith(parts[0], _comparison);
                }
                
                // Pattern with * in the middle (e.g., "user.*.delete")
                if (parts.Length == 2)
                {
                    return method.StartsWith(parts[0], _comparison) 
                        && method.EndsWith(parts[1], _comparison);
                }
            }

            // Exact match
            return method.Equals(pattern, _comparison);
        }
    }

    /// <summary>
    /// Extension methods for easy method whitelist middleware registration.
    /// </summary>
    public static class MethodWhitelistMiddlewareExtensions
    {
        /// <summary>
        /// Adds method whitelist middleware to the RPC endpoint.
        /// </summary>
        /// <param name="endpoint">The RPC endpoint.</param>
        /// <param name="allowedMethods">List of allowed method names.</param>
        /// <returns>The endpoint for chaining.</returns>
        public static RpcEndpoint UseMethodWhitelist(this RpcEndpoint endpoint, params string[] allowedMethods)
        {
            endpoint.GetMiddleware()?.Add(new MethodWhitelistMiddleware(allowedMethods));
            return endpoint;
        }

        /// <summary>
        /// Adds method whitelist middleware with inline configuration.
        /// </summary>
        /// <param name="endpoint">The RPC endpoint.</param>
        /// <param name="configure">Configuration action.</param>
        /// <returns>The endpoint for chaining.</returns>
        public static RpcEndpoint UseMethodWhitelist(this RpcEndpoint endpoint, Action<MethodWhitelistOptions> configure)
        {
            var options = new MethodWhitelistOptions();
            configure(options);
            endpoint.GetMiddleware()?.Add(new MethodWhitelistMiddleware(options));
            return endpoint;
        }

        /// <summary>
        /// Adds method blacklist middleware (denies specific methods).
        /// </summary>
        /// <param name="endpoint">The RPC endpoint.</param>
        /// <param name="deniedMethods">List of denied method names.</param>
        /// <returns>The endpoint for chaining.</returns>
        public static RpcEndpoint UseMethodBlacklist(this RpcEndpoint endpoint, params string[] deniedMethods)
        {
            endpoint.GetMiddleware()?.Add(new MethodWhitelistMiddleware(new MethodWhitelistOptions
            {
                DeniedMethods = deniedMethods.ToList()
            }));
            return endpoint;
        }
    }
}
