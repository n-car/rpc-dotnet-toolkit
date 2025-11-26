using System;
using System.Threading.Tasks;

namespace RpcToolkit.Middleware
{
    /// <summary>
    /// CORS middleware for cross-origin requests
    /// </summary>
    public class CorsMiddleware : IMiddleware
    {
        private readonly CorsOptions _options;

        /// <summary>
        /// Creates a new CORS middleware instance
        /// </summary>
        /// <param name="options">CORS configuration options</param>
        public CorsMiddleware(CorsOptions? options = null)
        {
            _options = options ?? new CorsOptions();
        }

        /// <summary>
        /// Executes before request processing (CORS headers handled at HTTP level)
        /// </summary>
        /// <param name="request">The RPC request</param>
        /// <param name="context">Request context</param>
        public Task BeforeAsync(RpcRequest request, object? context)
        {
            // CORS headers are typically set at HTTP layer, not RPC layer
            // This middleware can be used to validate origin if needed
            return Task.CompletedTask;
        }

        /// <summary>
        /// Executes after request processing (no-op for CORS middleware)
        /// </summary>
        /// <param name="request">The RPC request</param>
        /// <param name="result">The request result</param>
        /// <param name="context">Request context</param>
        public Task AfterAsync(RpcRequest request, object? result, object? context)
        {
            // CORS headers added via HTTP response (see ASP.NET Core integration)
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// CORS configuration options
    /// </summary>
    public class CorsOptions
    {
        /// <summary>
        /// Allowed origins (* for all)
        /// </summary>
        public string[] AllowedOrigins { get; set; } = new[] { "*" };

        /// <summary>
        /// Allowed HTTP methods
        /// </summary>
        public string[] AllowedMethods { get; set; } = new[] { "GET", "POST", "OPTIONS" };

        /// <summary>
        /// Allowed headers
        /// </summary>
        public string[] AllowedHeaders { get; set; } = new[] { "Content-Type", "Authorization" };

        /// <summary>
        /// Allow credentials
        /// </summary>
        public bool AllowCredentials { get; set; } = false;

        /// <summary>
        /// Max age in seconds
        /// </summary>
        public int MaxAge { get; set; } = 86400;
    }
}
