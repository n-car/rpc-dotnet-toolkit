using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RpcToolkit.Middleware
{
    /// <summary>
    /// Manages the middleware pipeline
    /// </summary>
    public class MiddlewareManager
    {
        private readonly List<IMiddleware> _beforeMiddleware = new();
        private readonly List<IMiddleware> _afterMiddleware = new();

        /// <summary>
        /// Add middleware to the pipeline
        /// </summary>
        /// <param name="middleware">Middleware instance</param>
        /// <param name="phase">When to execute: "before" or "after"</param>
        public MiddlewareManager Add(IMiddleware middleware, string phase = "before")
        {
            if (middleware == null)
                throw new ArgumentNullException(nameof(middleware));

            if (phase.ToLowerInvariant() == "before")
            {
                _beforeMiddleware.Add(middleware);
            }
            else if (phase.ToLowerInvariant() == "after")
            {
                _afterMiddleware.Add(middleware);
            }
            else
            {
                throw new ArgumentException($"Invalid phase '{phase}'. Must be 'before' or 'after'", nameof(phase));
            }

            return this;
        }

        /// <summary>
        /// Execute all "before" middleware
        /// </summary>
        public async Task ExecuteBeforeAsync(RpcRequest request, object? context)
        {
            foreach (var middleware in _beforeMiddleware)
            {
                await middleware.BeforeAsync(request, context);
            }
        }

        /// <summary>
        /// Execute all "after" middleware
        /// </summary>
        public async Task ExecuteAfterAsync(RpcRequest request, object? result, object? context)
        {
            // Execute in reverse order for after middleware
            foreach (var middleware in _afterMiddleware.AsEnumerable().Reverse())
            {
                await middleware.AfterAsync(request, result, context);
            }
        }

        /// <summary>
        /// Get count of registered middleware
        /// </summary>
        public int Count => _beforeMiddleware.Count + _afterMiddleware.Count;
    }
}
