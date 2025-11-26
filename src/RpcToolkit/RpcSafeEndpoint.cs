using System;
using Microsoft.Extensions.Logging;

namespace RpcToolkit
{
    /// <summary>
    /// RpcSafeEndpoint - Convenience class with Safe Mode preset enabled
    /// </summary>
    /// <remarks>
    /// This class extends RpcEndpoint and automatically enables Safe Mode,
    /// providing a cleaner API for safe RPC endpoints without manually
    /// setting SafeEnabled in options.
    /// 
    /// Safe Mode enables safe serialization of special types like DateTime,
    /// NaN, Infinity, and provides better type preservation across RPC calls.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Instead of:
    /// var rpc = new RpcEndpoint(context, new RpcOptions { SafeEnabled = true });
    /// 
    /// // Use:
    /// var rpc = new RpcSafeEndpoint(context);
    /// </code>
    /// </example>
    public class RpcSafeEndpoint : RpcEndpoint
    {
        /// <summary>
        /// Create a new RPC endpoint with Safe Mode enabled by default
        /// </summary>
        /// <param name="context">Context object passed to method handlers</param>
        /// <param name="options">Configuration options (SafeEnabled is preset to true)</param>
        /// <param name="logger">Optional logger</param>
        public RpcSafeEndpoint(object? context = null, RpcOptions? options = null, ILogger? logger = null)
            : base(context, MergeSafeOptions(options), logger)
        {
        }

        /// <summary>
        /// Merges user options with safe defaults
        /// </summary>
        private static RpcOptions MergeSafeOptions(RpcOptions? userOptions)
        {
            var safeOptions = userOptions ?? new RpcOptions();
            safeOptions.SafeEnabled = true;
            return safeOptions;
        }
    }
}
