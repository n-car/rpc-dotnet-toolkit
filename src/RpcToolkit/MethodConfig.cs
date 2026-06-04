using System;
using System.Threading.Tasks;

namespace RpcToolkit
{
    /// <summary>
    /// Configuration for a registered RPC method
    /// </summary>
    public class MethodConfig
    {
        /// <summary>
        /// Method name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Method handler function
        /// </summary>
        public Func<object?, object?, Task<object?>> Handler { get; set; } = null!;

        /// <summary>
        /// JSON schema for parameter validation
        /// </summary>
        public object? Schema { get; set; }

        /// <summary>
        /// Whether to expose this method's schema in introspection
        /// </summary>
        public bool ExposeSchema { get; set; } = false;

        /// <summary>
        /// Human-readable description of the method
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Scopes required to invoke this method. All listed scopes are required.
        /// </summary>
        public string[] RequiredScopes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Roles allowed to invoke this method. A principal in any listed role is allowed.
        /// </summary>
        public string[] RequiredRoles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Optional synchronous authorization policy.
        /// </summary>
        public Func<RpcAuthorizationContext, bool>? Authorize { get; set; }

        /// <summary>
        /// Optional asynchronous authorization policy.
        /// </summary>
        public Func<RpcAuthorizationContext, Task<bool>>? AuthorizeAsync { get; set; }
    }

    /// <summary>
    /// Internal method handler wrapper (backward compatibility)
    /// </summary>
    internal class MethodHandler
    {
        public string Name { get; set; } = string.Empty;
        public Func<object?, object?, Task<object?>> Handler { get; set; } = null!;
        public object? Schema { get; set; }
        public bool ExposeSchema { get; set; }
        public string? Description { get; set; }
        public string[] RequiredScopes { get; set; } = Array.Empty<string>();
        public string[] RequiredRoles { get; set; } = Array.Empty<string>();
        public Func<RpcAuthorizationContext, bool>? Authorize { get; set; }
        public Func<RpcAuthorizationContext, Task<bool>>? AuthorizeAsync { get; set; }
    }
}
