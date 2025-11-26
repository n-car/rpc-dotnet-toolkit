using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpcToolkit
{
    /// <summary>
    /// Options for configuring the RPC endpoint
    /// </summary>
    public class RpcOptions
    {
        /// <summary>
        /// Enable safe serialization with type prefixes (S: for strings, D: for dates)
        /// </summary>
        public bool SafeEnabled { get; set; } = false;

        /// <summary>
        /// Show warnings when BigInt/Date objects serialized without safe mode
        /// </summary>
        public bool WarnOnUnsafe { get; set; } = true;

        /// <summary>
        /// Enable batch request processing
        /// </summary>
        public bool EnableBatch { get; set; } = true;

        /// <summary>
        /// Maximum number of requests in a batch
        /// </summary>
        public int MaxBatchSize { get; set; } = 100;

        /// <summary>
        /// Enable structured logging
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Enable middleware system
        /// </summary>
        public bool EnableMiddleware { get; set; } = true;

        /// <summary>
        /// Enable schema validation
        /// </summary>
        public bool EnableValidation { get; set; } = true;

        /// <summary>
        /// Sanitize error messages in production
        /// </summary>
        public bool SanitizeErrors { get; set; } = true;

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Options for configuring the RPC client
    /// </summary>
    public class RpcClientOptions
    {
        /// <summary>
        /// Request timeout
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Enable safe serialization
        /// </summary>
        public bool SafeEnabled { get; set; } = false;

        /// <summary>
        /// Show warnings for unsafe serialization
        /// </summary>
        public bool WarnOnUnsafe { get; set; } = true;

        /// <summary>
        /// Verify SSL certificates
        /// </summary>
        public bool VerifySSL { get; set; } = true;

        /// <summary>
        /// Custom headers to include in requests
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// JSON-RPC 2.0 request
    /// </summary>
    public class RpcRequest
    {
        public string JsonRpc { get; set; } = "2.0";
        public string Method { get; set; } = string.Empty;
        public object? Params { get; set; }
        public object? Id { get; set; }

        public RpcRequest() { }

        public RpcRequest(string method, object? parameters = null, object? id = null)
        {
            Method = method;
            Params = parameters;
            Id = id;
        }

        /// <summary>
        /// Check if this is a notification (no ID)
        /// </summary>
        public bool IsNotification => Id == null;
    }

    /// <summary>
    /// JSON-RPC 2.0 response
    /// </summary>
    public class RpcResponse
    {
        public string JsonRpc { get; set; } = "2.0";
        public object? Result { get; set; }
        public RpcError? Error { get; set; }
        public object? Id { get; set; }

        /// <summary>
        /// Check if response has an error
        /// </summary>
        public bool HasError => Error != null;
    }

    /// <summary>
    /// JSON-RPC 2.0 error
    /// </summary>
    public class RpcError
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }

        public RpcError() { }

        public RpcError(int code, string message, object? data = null)
        {
            Code = code;
            Message = message;
            Data = data;
        }
    }

    /// <summary>
    /// Standard JSON-RPC 2.0 error codes
    /// </summary>
    public static class RpcErrorCodes
    {
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;

        // Implementation-specific errors (range -32000 to -32099)
        public const int ServerError = -32000;
        public const int AuthenticationError = -32001;
        public const int RateLimitExceeded = -32002;
        public const int ValidationError = -32003;
    }
}
