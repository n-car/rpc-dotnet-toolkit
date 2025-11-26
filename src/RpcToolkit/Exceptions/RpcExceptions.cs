using System;

namespace RpcToolkit.Exceptions
{
    /// <summary>
    /// Base class for all RPC exceptions
    /// </summary>
    public abstract class RpcException : Exception
    {
        /// <summary>
        /// JSON-RPC error code
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// Additional error data
        /// </summary>
        public new object? Data { get; set; }

        /// <summary>
        /// Creates a new RPC exception
        /// </summary>
        /// <param name="code">JSON-RPC error code</param>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional additional error data</param>
        protected RpcException(int code, string message, object? data = null)
            : base(message)
        {
            Code = code;
            Data = data;
        }

        /// <summary>
        /// Creates a new RPC exception with an inner exception
        /// </summary>
        /// <param name="code">JSON-RPC error code</param>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception that caused this error</param>
        /// <param name="data">Optional additional error data</param>
        protected RpcException(int code, string message, Exception innerException, object? data = null)
            : base(message, innerException)
        {
            Code = code;
            Data = data;
        }

        /// <summary>
        /// Convert to RpcError object
        /// </summary>
        public RpcError ToRpcError()
        {
            return new RpcError(Code, Message, Data);
        }
    }

    /// <summary>
    /// Parse error - Invalid JSON
    /// </summary>
    public class ParseErrorException : RpcException
    {
        /// <summary>
        /// Creates a new parse error exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional additional error data</param>
        public ParseErrorException(string message = "Parse error", object? data = null)
            : base(RpcErrorCodes.ParseError, message, data)
        {
        }

        /// <summary>
        /// Creates a new parse error exception from an inner exception
        /// </summary>
        /// <param name="innerException">The exception that caused the parse error</param>
        /// <param name="data">Optional additional error data</param>
        public ParseErrorException(Exception innerException, object? data = null)
            : base(RpcErrorCodes.ParseError, "Parse error", innerException, data)
        {
        }
    }

    /// <summary>
    /// Invalid Request - JSON is not a valid Request object
    /// </summary>
    public class InvalidRequestException : RpcException
    {
        /// <summary>
        /// Creates a new invalid request exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional additional error data</param>
        public InvalidRequestException(string message = "Invalid request", object? data = null)
            : base(RpcErrorCodes.InvalidRequest, message, data)
        {
        }
    }

    /// <summary>
    /// Method not found
    /// </summary>
    public class MethodNotFoundException : RpcException
    {
        /// <summary>
        /// Creates a new method not found exception
        /// </summary>
        /// <param name="method">The method name that was not found</param>
        public MethodNotFoundException(string method)
            : base(RpcErrorCodes.MethodNotFound, $"Method not found: {method}")
        {
        }

        /// <summary>
        /// Creates a new method not found exception with custom message
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional additional error data</param>
        public MethodNotFoundException(string message, object? data)
            : base(RpcErrorCodes.MethodNotFound, message, data)
        {
        }
    }

    /// <summary>
    /// Invalid method parameters
    /// </summary>
    public class InvalidParamsException : RpcException
    {
        /// <summary>
        /// Creates a new invalid params exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional additional error data</param>
        public InvalidParamsException(string message = "Invalid params", object? data = null)
            : base(RpcErrorCodes.InvalidParams, message, data)
        {
        }
    }

    /// <summary>
    /// Internal error
    /// </summary>
    public class InternalErrorException : RpcException
    {
        /// <summary>
        /// Creates a new internal error exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional additional error data</param>
        public InternalErrorException(string message = "Internal error", object? data = null)
            : base(RpcErrorCodes.InternalError, message, data)
        {
        }

        /// <summary>
        /// Creates a new internal error exception from an inner exception
        /// </summary>
        /// <param name="innerException">The exception that caused the internal error</param>
        public InternalErrorException(Exception innerException)
            : base(RpcErrorCodes.InternalError, "Internal error", innerException)
        {
        }
    }

    /// <summary>
    /// Server error
    /// </summary>
    public class ServerErrorException : RpcException
    {
        /// <summary>
        /// Creates a new server error exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional additional error data</param>
        public ServerErrorException(string message = "Server error", object? data = null)
            : base(RpcErrorCodes.ServerError, message, data)
        {
        }
    }

    /// <summary>
    /// Authentication error
    /// </summary>
    public class AuthenticationErrorException : RpcException
    {
        /// <summary>
        /// Creates a new authentication error exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional additional error data</param>
        public AuthenticationErrorException(string message = "Authentication required", object? data = null)
            : base(RpcErrorCodes.AuthenticationError, message, data)
        {
        }
    }

    /// <summary>
    /// Rate limit exceeded
    /// </summary>
    public class RateLimitExceededException : RpcException
    {
        /// <summary>
        /// Creates a new rate limit exceeded exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional additional error data</param>
        public RateLimitExceededException(string message = "Rate limit exceeded", object? data = null)
            : base(RpcErrorCodes.RateLimitExceeded, message, data)
        {
        }
    }

    /// <summary>
    /// Validation error
    /// </summary>
    public class ValidationErrorException : RpcException
    {
        /// <summary>
        /// Creates a new validation error exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional validation error details</param>
        public ValidationErrorException(string message, object? data = null)
            : base(RpcErrorCodes.ValidationError, message, data)
        {
        }
    }
}
