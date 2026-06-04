using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Claims;
using System.Security.Principal;
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

            var existingUser = ExtractAuthenticatedUser(context);
            if (existingUser != null)
            {
                StoreAuthenticatedUser(context, existingUser);
                return;
            }

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

                StoreAuthenticatedUser(context, user);
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
            var authHeader = ExtractAuthorizationHeader(context);

            if (authHeader == null || authHeader.Length == 0)
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

        private static object? ExtractAuthenticatedUser(object? context)
        {
            if (context == null)
                return null;

            if (context is RpcRequestContext rpcContext)
                return NormalizeAuthenticatedUser(rpcContext.Principal ?? rpcContext.User);

            var userProperty = context.GetType().GetProperty("User");
            var user = NormalizeAuthenticatedUser(userProperty?.GetValue(context));
            if (user != null)
                return user;

            var httpContextProperty = context.GetType().GetProperty("HttpContext");
            var httpContext = httpContextProperty?.GetValue(context);
            if (httpContext != null && !ReferenceEquals(httpContext, context))
            {
                userProperty = httpContext.GetType().GetProperty("User");
                user = NormalizeAuthenticatedUser(userProperty?.GetValue(httpContext));
                if (user != null)
                    return user;
            }

            return null;
        }

        private static object? NormalizeAuthenticatedUser(object? user)
        {
            if (user == null)
                return null;

            if (user is IPrincipal principal)
            {
                return principal.Identity?.IsAuthenticated == true ? user : null;
            }

            return user;
        }

        private static string? ExtractAuthorizationHeader(object? context)
        {
            if (context == null)
                return null;

            if (context is RpcRequestContext rpcContext)
            {
                return rpcContext.Authorization ?? rpcContext.GetHeader("Authorization");
            }

            if (TryGetHeaderValue(context, "Authorization", out var authorization))
                return authorization;

            var authProperty = context.GetType().GetProperty("Authorization");
            var authValue = authProperty?.GetValue(context)?.ToString();
            if (!string.IsNullOrEmpty(authValue))
                return authValue;

            var headersProperty = context.GetType().GetProperty("Headers");
            var headers = headersProperty?.GetValue(context);
            if (TryGetHeaderValue(headers, "Authorization", out authorization))
                return authorization;

            var requestProperty = context.GetType().GetProperty("Request");
            var request = requestProperty?.GetValue(context);
            if (request != null)
            {
                headersProperty = request.GetType().GetProperty("Headers");
                headers = headersProperty?.GetValue(request);
                if (TryGetHeaderValue(headers, "Authorization", out authorization))
                    return authorization;
            }

            var httpContextProperty = context.GetType().GetProperty("HttpContext");
            var httpContext = httpContextProperty?.GetValue(context);
            if (httpContext != null && !ReferenceEquals(httpContext, context))
            {
                return ExtractAuthorizationHeader(httpContext);
            }

            return null;
        }

        private static bool TryGetHeaderValue(object? headers, string name, out string? value)
        {
            value = null;

            if (headers == null)
                return false;

            if (headers is IDictionary<string, string> stringDictionary)
            {
                foreach (var header in stringDictionary)
                {
                    if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = header.Value;
                        return !string.IsNullOrEmpty(value);
                    }
                }
            }

            if (headers is IReadOnlyDictionary<string, string> readOnlyStringDictionary)
            {
                foreach (var header in readOnlyStringDictionary)
                {
                    if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = header.Value;
                        return !string.IsNullOrEmpty(value);
                    }
                }
            }

            if (headers is NameValueCollection nameValueCollection)
            {
                value = nameValueCollection[name];
                return !string.IsNullOrEmpty(value);
            }

            if (headers is IDictionary dictionary)
            {
                foreach (DictionaryEntry header in dictionary)
                {
                    if (header.Key is string key && string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = header.Value?.ToString();
                        return !string.IsNullOrEmpty(value);
                    }
                }
            }

            var indexer = headers.GetType().GetProperty("Item", new[] { typeof(string) });
            var indexedValue = indexer?.GetValue(headers, new object[] { name })?.ToString();
            if (!string.IsNullOrEmpty(indexedValue))
            {
                value = indexedValue;
                return true;
            }

            if (!(headers is string) && headers is IEnumerable enumerable)
            {
                foreach (var header in enumerable)
                {
                    if (header == null)
                        continue;

                    var keyProperty = header.GetType().GetProperty("Key");
                    var valueProperty = header.GetType().GetProperty("Value");
                    var key = keyProperty?.GetValue(header)?.ToString();

                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = valueProperty?.GetValue(header)?.ToString();
                        return !string.IsNullOrEmpty(value);
                    }
                }
            }

            return false;
        }

        private static void StoreAuthenticatedUser(object? context, object? user)
        {
            if (context == null || user == null)
                return;

            if (context is RpcRequestContext rpcContext)
            {
                rpcContext.User = user;
                if (user is ClaimsPrincipal claimsPrincipal)
                {
                    rpcContext.Principal = claimsPrincipal;
                }

                return;
            }

            var userProperty = context.GetType().GetProperty("User");
            if (userProperty == null || !userProperty.CanWrite || userProperty.GetIndexParameters().Length != 0)
                return;

            var userType = user.GetType();
            if (userProperty.PropertyType == typeof(object) || userProperty.PropertyType.IsAssignableFrom(userType))
            {
                userProperty.SetValue(context, user);
            }
        }
    }
}
