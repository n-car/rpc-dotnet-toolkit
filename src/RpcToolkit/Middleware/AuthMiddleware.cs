using System;
using System.Threading.Tasks;
using RpcToolkit.Exceptions;

namespace RpcToolkit.Middleware
{
    /// <summary>
    /// Authentication middleware
    /// </summary>
    public class AuthMiddleware : IMiddleware
    {
        private readonly Func<string, Task<object?>> _authenticator;
        private readonly string[] _allowedMethods;
        private readonly bool _required;

        /// <summary>
        /// Create authentication middleware
        /// </summary>
        /// <param name="authenticator">Function to validate token and return user object</param>
        /// <param name="allowedMethods">Methods that don't require auth (whitelist)</param>
        /// <param name="required">Whether auth is required for non-whitelisted methods</param>
        public AuthMiddleware(
            Func<string, Task<object?>> authenticator,
            string[]? allowedMethods = null,
            bool required = true)
        {
            _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            _allowedMethods = allowedMethods ?? Array.Empty<string>();
            _required = required;
        }

        /// <summary>
        /// Synchronous authenticator overload
        /// </summary>
        public AuthMiddleware(
            Func<string, object?> authenticator,
            string[]? allowedMethods = null,
            bool required = true)
            : this(token => Task.FromResult(authenticator(token)), allowedMethods, required)
        {
        }

        /// <summary>
        /// Validates authentication before request processing
        /// </summary>
        /// <param name="request">The RPC request</param>
        /// <param name="context">Request context containing authentication information</param>
        public async Task BeforeAsync(RpcRequest request, object? context)
        {
            // Check if method is whitelisted
            foreach (var method in _allowedMethods)
            {
                if (request.Method.Equals(method, StringComparison.OrdinalIgnoreCase))
                {
                    return; // Bypass auth
                }
            }

            // Extract token from context
            var token = ExtractToken(context);

            if (string.IsNullOrEmpty(token) && _required)
            {
                throw new AuthenticationErrorException("Authentication required");
            }

            if (!string.IsNullOrEmpty(token))
            {
                // Authenticate
                var user = await _authenticator(token!);

                if (user == null && _required)
                {
                    throw new AuthenticationErrorException("Invalid authentication token");
                }

                // Store authenticated user in context
                if (user != null && context != null)
                {
                    // In real implementation, add user to context
                    // context.User = user;
                }
            }
        }

        /// <summary>
        /// Executes after request processing (no-op for auth middleware)
        /// </summary>
        /// <param name="request">The RPC request</param>
        /// <param name="result">The request result</param>
        /// <param name="context">Request context</param>
        public Task AfterAsync(RpcRequest request, object? result, object? context)
        {
            return Task.CompletedTask;
        }

        private string? ExtractToken(object? context)
        {
            // Try to get Authorization header from context
            // In ASP.NET Core, this would come from HttpContext
            var authHeader = context?.GetType().GetProperty("Authorization")?.GetValue(context)?.ToString();

            if (string.IsNullOrEmpty(authHeader))
            {
                return null;
            }

            // Extract Bearer token
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring(7);
            }

            return authHeader;
        }
    }
}
