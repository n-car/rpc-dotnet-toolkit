using Microsoft.AspNetCore.Http;
using RpcToolkit;
using System.Text;

namespace RpcToolkit.AspNetCore;

/// <summary>
/// ASP.NET Core middleware for handling JSON-RPC requests
/// </summary>
public class RpcMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RpcEndpoint _endpoint;
    private readonly RpcMiddlewareOptions _options;

    public RpcMiddleware(RequestDelegate next, RpcEndpoint endpoint, RpcMiddlewareOptions? options = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _options = options ?? new RpcMiddlewareOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if this is an RPC request
        if (!IsRpcRequest(context))
        {
            await _next(context);
            return;
        }

        // Only allow POST requests
        if (context.Request.Method != "POST")
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            await context.Response.WriteAsync("Method Not Allowed");
            return;
        }

        // Set CORS headers if enabled
        if (_options.EnableCors)
        {
            SetCorsHeaders(context);
            
            // Handle preflight request
            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }
        }

        try
        {
            // Read request body
            string requestBody;
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            // Handle RPC request
            var response = await _endpoint.HandleRequestAsync(requestBody);

            // Write response
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync(response);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";
            
            var errorResponse = @"{""jsonrpc"":""2.0"",""error"":{""code"":-32603,""message"":""Internal error""},""id"":null}";
            
            if (_options.IncludeExceptionDetails)
            {
                errorResponse = $@"{{""jsonrpc"":""2.0"",""error"":{{""code"":-32603,""message"":""{ex.Message}""}},""id"":null}}";
            }
            
            await context.Response.WriteAsync(errorResponse);
        }
    }

    private bool IsRpcRequest(HttpContext context)
    {
        // Match path
        if (_options.Path.HasValue && !context.Request.Path.StartsWithSegments(_options.Path.Value))
        {
            return false;
        }

        // Check content type for POST requests
        if (context.Request.Method == "POST")
        {
            var contentType = context.Request.ContentType?.Split(';')[0].Trim();
            return contentType == "application/json" || contentType == "application/json-rpc";
        }

        return false;
    }

    private void SetCorsHeaders(HttpContext context)
    {
        var origin = context.Request.Headers["Origin"].ToString();
        
        if (!string.IsNullOrEmpty(origin) && IsOriginAllowed(origin))
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", origin);
            context.Response.Headers.Append("Access-Control-Allow-Methods", "POST, OPTIONS");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
            
            if (_options.AllowCredentials)
            {
                context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
            }
        }
    }

    private bool IsOriginAllowed(string origin)
    {
        if (_options.AllowedOrigins == null || _options.AllowedOrigins.Length == 0)
        {
            return true;
        }

        return _options.AllowedOrigins.Contains(origin) || _options.AllowedOrigins.Contains("*");
    }

    private object CreateRpcContext(HttpContext httpContext)
    {
        return new
        {
            HttpContext = httpContext,
            Request = httpContext.Request,
            Response = httpContext.Response,
            User = httpContext.User,
            Connection = httpContext.Connection,
            RemoteIp = httpContext.Connection.RemoteIpAddress?.ToString(),
            Headers = httpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            Query = httpContext.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString())
        };
    }
}

/// <summary>
/// Options for RPC middleware
/// </summary>
public class RpcMiddlewareOptions
{
    /// <summary>
    /// Path to match for RPC requests (e.g., "/rpc"). If null, matches any path.
    /// </summary>
    public PathString? Path { get; set; } = "/rpc";

    /// <summary>
    /// Enable CORS support
    /// </summary>
    public bool EnableCors { get; set; } = true;

    /// <summary>
    /// Allowed origins for CORS. Use "*" to allow all.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = { "*" };

    /// <summary>
    /// Allow credentials in CORS requests
    /// </summary>
    public bool AllowCredentials { get; set; } = false;

    /// <summary>
    /// Include exception details in error responses (for development)
    /// </summary>
    public bool IncludeExceptionDetails { get; set; } = false;
}
