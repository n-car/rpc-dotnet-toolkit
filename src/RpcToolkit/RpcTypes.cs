using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using RpcToolkit.Logging;
#if NETSTANDARD2_0
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

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
        /// Advanced batch processing options
        /// </summary>
        public RpcBatchOptions BatchOptions { get; set; } = new RpcBatchOptions();

        /// <summary>
        /// Enable structured logging
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Logging configuration options
        /// </summary>
        public RpcLoggerOptions LoggerOptions { get; set; } = new RpcLoggerOptions();

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

        /// <summary>
        /// Enable introspection methods (__rpc.*)
        /// </summary>
        public bool EnableIntrospection { get; set; } = false;

        /// <summary>
        /// Prefix for introspection methods
        /// </summary>
        public string IntrospectionPrefix { get; set; } = "__rpc";
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
    /// Per-request context passed to RPC middleware and method handlers.
    /// </summary>
    public class RpcRequestContext
    {
        /// <summary>
        /// Authorization header value for the current request.
        /// </summary>
        public string? Authorization { get; set; }

        /// <summary>
        /// Request headers, matched case-insensitively.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Authenticated user populated by authentication middleware.
        /// </summary>
        public object? User { get; set; }

        /// <summary>
        /// Authenticated claims principal for authorization policies.
        /// </summary>
        public ClaimsPrincipal? Principal { get; set; }

        /// <summary>
        /// Optional platform-specific HTTP context.
        /// </summary>
        public object? HttpContext { get; set; }

        /// <summary>
        /// Optional platform-specific request object.
        /// </summary>
        public object? Request { get; set; }

        /// <summary>
        /// Optional platform-specific response object.
        /// </summary>
        public object? Response { get; set; }

        /// <summary>
        /// Remote IP address for the current request.
        /// </summary>
        public string? RemoteIp { get; set; }

        /// <summary>
        /// Query string values for the current request.
        /// </summary>
        public Dictionary<string, string> Query { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Creates an empty request context.
        /// </summary>
        public RpcRequestContext()
        {
        }

        /// <summary>
        /// Creates a request context with the specified headers.
        /// </summary>
        public RpcRequestContext(IDictionary<string, string>? headers)
        {
            if (headers == null)
                return;

            foreach (var header in headers)
            {
                Headers[header.Key] = header.Value;
            }

            if (Headers.TryGetValue("Authorization", out var authorization))
            {
                Authorization = authorization;
            }
        }

        /// <summary>
        /// Get a request header by name.
        /// </summary>
        public string? GetHeader(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            return Headers.TryGetValue(name, out var value) ? value : null;
        }
    }

    /// <summary>
    /// Context passed to method-level authorization policies.
    /// </summary>
    public class RpcAuthorizationContext
    {
        /// <summary>
        /// Current JSON-RPC request.
        /// </summary>
        public RpcRequest Request { get; }

        /// <summary>
        /// Registered method configuration.
        /// </summary>
        public MethodConfig Method { get; }

        /// <summary>
        /// Typed RPC request context, if available.
        /// </summary>
        public RpcRequestContext? Context { get; }

        /// <summary>
        /// Original context object passed to the endpoint.
        /// </summary>
        public object? RawContext { get; }

        /// <summary>
        /// Authenticated claims principal, if available.
        /// </summary>
        public ClaimsPrincipal? Principal { get; }

        /// <summary>
        /// Creates a new authorization context.
        /// </summary>
        public RpcAuthorizationContext(
            RpcRequest request,
            MethodConfig method,
            object? context,
            ClaimsPrincipal? principal)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Method = method ?? throw new ArgumentNullException(nameof(method));
            RawContext = context;
            Context = context as RpcRequestContext;
            Principal = principal;
        }
    }

    /// <summary>
    /// JSON-RPC 2.0 request
    /// </summary>
    public class RpcRequest
    {
        /// <summary>JSON-RPC protocol version (always "2.0")</summary>
#if NETSTANDARD2_0
        [JsonProperty("jsonrpc")]
#else
        [JsonPropertyName("jsonrpc")]
#endif
        public string JsonRpc { get; set; } = "2.0";
        
        /// <summary>Name of the method to invoke</summary>
#if NETSTANDARD2_0
        [JsonProperty("method")]
#else
        [JsonPropertyName("method")]
#endif
        public string Method { get; set; } = string.Empty;
        
        /// <summary>Method parameters (object or array)</summary>
#if NETSTANDARD2_0
        [JsonProperty("params")]
#else
        [JsonPropertyName("params")]
#endif
        public object? Params { get; set; }
        
        /// <summary>Request identifier (null for notifications)</summary>
#if NETSTANDARD2_0
        [JsonProperty("id")]
#else
        [JsonPropertyName("id")]
#endif
        public object? Id { get; set; }

        /// <summary>
        /// Creates a new empty RPC request
        /// </summary>
        public RpcRequest() { }

        /// <summary>
        /// Creates a new RPC request with the specified method and parameters
        /// </summary>
        /// <param name="method">Method name to invoke</param>
        /// <param name="parameters">Method parameters</param>
        /// <param name="id">Request identifier</param>
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
        /// <summary>JSON-RPC protocol version (always "2.0")</summary>
#if NETSTANDARD2_0
        [JsonProperty("jsonrpc")]
#else
        [JsonPropertyName("jsonrpc")]
#endif
        public string JsonRpc { get; set; } = "2.0";
        
        /// <summary>Successful result (mutually exclusive with Error)</summary>
#if NETSTANDARD2_0
        [JsonProperty("result")]
#else
        [JsonPropertyName("result")]
#endif
        public object? Result { get; set; }
        
        /// <summary>Error information if request failed</summary>
#if NETSTANDARD2_0
        [JsonProperty("error")]
#else
        [JsonPropertyName("error")]
#endif
        public RpcError? Error { get; set; }
        
        /// <summary>Request identifier (matches the request Id)</summary>
#if NETSTANDARD2_0
        [JsonProperty("id")]
#else
        [JsonPropertyName("id")]
#endif
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
        /// <summary>Numeric error code</summary>
#if NETSTANDARD2_0
        [JsonProperty("code")]
#else
        [JsonPropertyName("code")]
#endif
        public int Code { get; set; }
        
        /// <summary>Human-readable error message</summary>
#if NETSTANDARD2_0
        [JsonProperty("message")]
#else
        [JsonPropertyName("message")]
#endif
        public string Message { get; set; } = string.Empty;
        
        /// <summary>Additional error details</summary>
#if NETSTANDARD2_0
        [JsonProperty("data")]
#else
        [JsonPropertyName("data")]
#endif
        public object? Data { get; set; }

        /// <summary>
        /// Creates a new empty RPC error
        /// </summary>
        public RpcError() { }

        /// <summary>
        /// Creates a new RPC error with the specified code and message
        /// </summary>
        /// <param name="code">Error code</param>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional additional error data</param>
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
        /// <summary>Invalid JSON received by the server (-32700)</summary>
        public const int ParseError = -32700;
        
        /// <summary>JSON is not a valid request object (-32600)</summary>
        public const int InvalidRequest = -32600;
        
        /// <summary>Method does not exist or is not available (-32601)</summary>
        public const int MethodNotFound = -32601;
        
        /// <summary>Invalid method parameters (-32602)</summary>
        public const int InvalidParams = -32602;
        
        /// <summary>Internal JSON-RPC error (-32603)</summary>
        public const int InternalError = -32603;

        // Implementation-specific errors (range -32000 to -32099)
        /// <summary>Generic server error (-32000)</summary>
        public const int ServerError = -32000;
        
        /// <summary>Authentication required or failed (-32001)</summary>
        public const int AuthenticationError = -32001;

        /// <summary>Authenticated caller is not authorized for the method (-32004)</summary>
        public const int AuthorizationError = -32004;
        
        /// <summary>Too many requests in a given time frame (-32002)</summary>
        public const int RateLimitExceeded = -32002;
        
        /// <summary>Request or response validation failed (-32003)</summary>
        public const int ValidationError = -32003;
    }
}
