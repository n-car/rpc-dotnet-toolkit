using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace RpcToolkit
{
    /// <summary>
    /// RpcSafeClient - Convenience class with Safe Mode preset enabled
    /// </summary>
    /// <remarks>
    /// This class extends RpcClient and automatically enables Safe Mode,
    /// providing a cleaner API for safe RPC clients without manually
    /// setting SafeEnabled in options.
    /// 
    /// Safe Mode enables safe deserialization of special types like DateTime,
    /// large integers (as strings), NaN, Infinity, and provides better type
    /// preservation across RPC calls.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Instead of:
    /// var client = new RpcClient("http://localhost:3000/api", new RpcClientOptions { SafeEnabled = true });
    /// 
    /// // Use:
    /// var client = new RpcSafeClient("http://localhost:3000/api");
    /// </code>
    /// </example>
    public class RpcSafeClient : RpcClient
    {
        /// <summary>
        /// Create a new RPC client with Safe Mode enabled by default
        /// </summary>
        /// <param name="baseUrl">Base URL of the RPC endpoint</param>
        /// <param name="options">Client options (SafeEnabled is preset to true)</param>
        /// <param name="logger">Optional logger</param>
        /// <param name="httpClient">Optional HttpClient instance (if null, creates one)</param>
        public RpcSafeClient(
            string baseUrl,
            RpcClientOptions? options = null,
            ILogger? logger = null,
            HttpClient? httpClient = null)
            : base(baseUrl, MergeSafeOptions(options), logger, httpClient)
        {
        }

        /// <summary>
        /// Merges user options with safe defaults
        /// </summary>
        private static RpcClientOptions MergeSafeOptions(RpcClientOptions? userOptions)
        {
            var safeOptions = userOptions ?? new RpcClientOptions();
            safeOptions.SafeEnabled = true;
            return safeOptions;
        }
    }
}
