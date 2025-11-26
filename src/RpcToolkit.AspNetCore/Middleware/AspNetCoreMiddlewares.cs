using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RpcToolkit.Middleware;

namespace RpcToolkit.AspNetCore.Middleware;

/// <summary>
/// Logging middleware for RPC requests
/// </summary>
public class RpcLoggingMiddleware : RpcToolkit.Middleware.IMiddleware
{
    private readonly ILogger _logger;

    public RpcLoggingMiddleware(ILogger<RpcLoggingMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task BeforeAsync(RpcRequest request, object? context)
    {
        var httpContext = ExtractHttpContext(context);
        var remoteIp = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        _logger.LogInformation(
            "RPC Request: {Method} from {RemoteIp}",
            request.Method,
            remoteIp
        );

        await Task.CompletedTask;
    }

    public async Task AfterAsync(RpcRequest request, object? result, object? context)
    {
        _logger.LogDebug("RPC Response: {Method} completed", request.Method);
        await Task.CompletedTask;
    }

    private HttpContext? ExtractHttpContext(object? context)
    {
        if (context == null) return null;
        
        var contextType = context.GetType();
        var httpContextProp = contextType.GetProperty("HttpContext");
        
        return httpContextProp?.GetValue(context) as HttpContext;
    }
}

/// <summary>
/// Request metrics middleware for RPC
/// </summary>
public class RpcMetricsMiddleware : RpcToolkit.Middleware.IMiddleware
{
    private readonly ILogger _logger;
    private readonly System.Diagnostics.Stopwatch _stopwatch = new();

    public RpcMetricsMiddleware(ILogger<RpcMetricsMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task BeforeAsync(RpcRequest request, object? context)
    {
        _stopwatch.Restart();
        await Task.CompletedTask;
    }

    public async Task AfterAsync(RpcRequest request, object? result, object? context)
    {
        _stopwatch.Stop();
        
        _logger.LogInformation(
            "RPC Metrics: {Method} executed in {ElapsedMs}ms",
            request.Method,
            _stopwatch.ElapsedMilliseconds
        );

        await Task.CompletedTask;
    }
}

/// <summary>
/// IP-based access control middleware
/// </summary>
public class IpWhitelistMiddleware : RpcToolkit.Middleware.IMiddleware
{
    private readonly HashSet<string> _allowedIps;
    private readonly ILogger _logger;

    public IpWhitelistMiddleware(string[] allowedIps, ILogger<IpWhitelistMiddleware> logger)
    {
        _allowedIps = new HashSet<string>(allowedIps ?? Array.Empty<string>());
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task BeforeAsync(RpcRequest request, object? context)
    {
        var httpContext = ExtractHttpContext(context);
        var remoteIp = httpContext?.Connection.RemoteIpAddress?.ToString();

        if (remoteIp == null || !_allowedIps.Contains(remoteIp))
        {
            _logger.LogWarning("Access denied for IP: {RemoteIp}", remoteIp ?? "unknown");
            throw new UnauthorizedAccessException($"IP {remoteIp} is not whitelisted");
        }

        await Task.CompletedTask;
    }

    public async Task AfterAsync(RpcRequest request, object? result, object? context)
    {
        await Task.CompletedTask;
    }

    private HttpContext? ExtractHttpContext(object? context)
    {
        if (context == null) return null;
        
        var contextType = context.GetType();
        var httpContextProp = contextType.GetProperty("HttpContext");
        
        return httpContextProp?.GetValue(context) as HttpContext;
    }
}
