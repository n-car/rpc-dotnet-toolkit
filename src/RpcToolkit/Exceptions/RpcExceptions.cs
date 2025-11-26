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
        public object? Data { get; set; }

        protected RpcException(int code, string message, object? data = null)
            : base(message)
        {
            Code = code;
            Data = data;
        }

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
        public ParseErrorException(string message = "Parse error", object? data = null)
            : base(RpcErrorCodes.ParseError, message, data)
        {
        }

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
        public InvalidRequestException(string message = "Invalid Request", object? data = null)
            : base(RpcErrorCodes.InvalidRequest, message, data)
        {
        }
    }

    /// <summary>
    /// Method not found
    /// </summary>
    public class MethodNotFoundException : RpcException
    {
        public MethodNotFoundException(string methodName)
            : base(RpcErrorCodes.MethodNotFound, $"Method not found: {methodName}")
        {
        }
    }

    /// <summary>
    /// Invalid method parameters
    /// </summary>
    public class InvalidParamsException : RpcException
    {
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
        public InternalErrorException(string message = "Internal error", object? data = null)
            : base(RpcErrorCodes.InternalError, message, data)
        {
        }

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
        public ValidationErrorException(string message, object? data = null)
            : base(RpcErrorCodes.ValidationError, message, data)
        {
        }
    }
}
