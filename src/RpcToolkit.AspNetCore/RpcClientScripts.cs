using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace RpcToolkit.AspNetCore;

/// <summary>
/// Serves the shared JavaScript RPC client assets embedded in RpcToolkit.AspNetCore.
/// </summary>
public static class RpcClientScriptsExtensions
{
    /// <summary>
    /// Serves the shared JavaScript RPC client under the specified path.
    /// </summary>
    public static IApplicationBuilder UseRpcClientScripts(
        this IApplicationBuilder app,
        string path = "/vendor/rpc-client")
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        var pathString = NormalizePath(path);

        return app.Map(pathString, builder =>
        {
            builder.Run(RpcClientScriptAssets.HandleRequestAsync);
        });
    }

    /// <summary>
    /// Maps endpoint-routing handlers for the shared JavaScript RPC client.
    /// </summary>
    public static IEndpointConventionBuilder MapRpcClientScripts(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/vendor/rpc-client/{file}")
    {
        if (endpoints == null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Route pattern cannot be empty.", nameof(pattern));
        }

        return endpoints.MapMethods(
            pattern,
            new[] { HttpMethods.Get, HttpMethods.Head },
            async context =>
            {
                var fileName = context.Request.RouteValues["file"]?.ToString();
                if (!await RpcClientScriptAssets.TryWriteAsync(context, fileName))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                }
            });
    }

    private static PathString NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be empty.", nameof(path));
        }

        return path.StartsWith("/", StringComparison.Ordinal)
            ? new PathString(path)
            : new PathString("/" + path);
    }
}

internal static class RpcClientScriptAssets
{
    private static readonly Assembly Assembly = typeof(RpcClientScriptAssets).Assembly;

    private static readonly IReadOnlyDictionary<string, string> ContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["rpc-client.cjs"] = "text/javascript; charset=utf-8",
            ["rpc-client.js"] = "text/javascript; charset=utf-8",
            ["rpc-client.min.js"] = "text/javascript; charset=utf-8",
            ["rpc-client.min.mjs"] = "text/javascript; charset=utf-8",
            ["rpc-client.mjs"] = "text/javascript; charset=utf-8"
        };

    public static async Task HandleRequestAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) &&
            !HttpMethods.IsHead(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        var fileName = context.Request.Path.Value?.Trim('/');
        if (!await TryWriteAsync(context, fileName))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }

    public static async Task<bool> TryWriteAsync(HttpContext context, string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName.Contains("/", StringComparison.Ordinal) ||
            fileName.Contains("\\", StringComparison.Ordinal) ||
            !ContentTypes.TryGetValue(fileName, out var contentType))
        {
            return false;
        }

        var resourceName = $"RpcToolkit.AspNetCore.ClientScripts.{fileName}";
        using var stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return false;
        }

        context.Response.ContentType = contentType;
        context.Response.ContentLength = stream.Length;
        context.Response.Headers["Cache-Control"] = "public, max-age=3600";

        if (HttpMethods.IsHead(context.Request.Method))
        {
            return true;
        }

        await stream.CopyToAsync(context.Response.Body);
        return true;
    }
}
