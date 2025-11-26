using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RpcToolkit;

namespace RpcToolkit.AspNetCore;

/// <summary>
/// Extension methods for configuring RPC services
/// </summary>
public static class RpcServiceExtensions
{
    /// <summary>
    /// Adds RPC endpoint as a singleton service
    /// </summary>
    public static IServiceCollection AddRpcEndpoint(
        this IServiceCollection services,
        Action<RpcEndpoint>? configure = null)
    {
        services.AddSingleton<RpcEndpoint>(provider =>
        {
            var endpoint = new RpcEndpoint();
            configure?.Invoke(endpoint);
            return endpoint;
        });

        return services;
    }

    /// <summary>
    /// Adds RPC endpoint with options
    /// </summary>
    public static IServiceCollection AddRpcEndpoint(
        this IServiceCollection services,
        RpcOptions options,
        Action<RpcEndpoint>? configure = null)
    {
        services.AddSingleton<RpcEndpoint>(provider =>
        {
            var endpoint = new RpcEndpoint(null, options);
            configure?.Invoke(endpoint);
            return endpoint;
        });

        return services;
    }

    /// <summary>
    /// Adds RPC endpoint with context and options
    /// </summary>
    public static IServiceCollection AddRpcEndpoint(
        this IServiceCollection services,
        Func<IServiceProvider, object> contextFactory,
        RpcOptions? options = null,
        Action<RpcEndpoint>? configure = null)
    {
        services.AddSingleton<RpcEndpoint>(provider =>
        {
            var context = contextFactory(provider);
            var endpoint = new RpcEndpoint(context, options);
            configure?.Invoke(endpoint);
            return endpoint;
        });

        return services;
    }

    /// <summary>
    /// Uses RPC middleware with default options
    /// </summary>
    public static IApplicationBuilder UseRpc(this IApplicationBuilder app)
    {
        return app.UseRpc(new RpcMiddlewareOptions());
    }

    /// <summary>
    /// Uses RPC middleware with custom options
    /// </summary>
    public static IApplicationBuilder UseRpc(
        this IApplicationBuilder app,
        RpcMiddlewareOptions options)
    {
        var endpoint = app.ApplicationServices.GetRequiredService<RpcEndpoint>();
        return app.UseMiddleware<RpcMiddleware>(endpoint, options);
    }

    /// <summary>
    /// Uses RPC middleware at specific path
    /// </summary>
    public static IApplicationBuilder UseRpc(
        this IApplicationBuilder app,
        string path)
    {
        return app.UseRpc(new RpcMiddlewareOptions { Path = path });
    }

    /// <summary>
    /// Maps RPC endpoint to a specific route
    /// </summary>
    public static IApplicationBuilder MapRpc(
        this IApplicationBuilder app,
        string path = "/rpc")
    {
        return app.Map(path, builder =>
        {
            builder.UseRpc(new RpcMiddlewareOptions { Path = null });
        });
    }
}
