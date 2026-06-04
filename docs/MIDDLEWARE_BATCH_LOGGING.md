# Middleware, Batch, Logging

## Middleware

Middleware runs around RPC method execution.

```csharp
using RpcToolkit;
using RpcToolkit.Middleware;

var endpoint = new RpcEndpoint();

endpoint.GetMiddleware()?.Add(new RateLimitMiddleware(100, 60, "global"), "before");
endpoint.UseTiming();
endpoint.UseMethodWhitelist("system.*", "modules.*");
```

Recommended order:

1. Authentication middleware, if used to populate identity from a token.
2. Method whitelist or blacklist middleware, if needed.
3. Timing middleware.
4. Rate limiting.

Method-level authorization via `MethodConfig.RequiredScopes` runs after method lookup and before handler invocation.

## Batch Options

```csharp
var endpoint = new RpcEndpoint(null, new RpcOptions
{
    EnableBatch = true,
    BatchOptions = new RpcBatchOptions
    {
        MaxSize = 50,
        Parallel = true,
        MaxParallelism = 4,
        ContinueOnError = true,
        CollectMetrics = true
    }
});
```

## Logging

```csharp
using RpcToolkit.Logging;

var endpoint = new RpcEndpoint(null, new RpcOptions
{
    EnableLogging = true,
    LoggerOptions = new RpcLoggerOptions
    {
        Level = RpcLogLevel.Info,
        Format = RpcLogFormat.Text,
        IncludeTimestamp = true
    }
});
```

## CORS

For ASP.NET Core, configure CORS at the HTTP adapter level:

```csharp
app.UseRpc(new RpcMiddlewareOptions
{
    Path = "/rpc",
    EnableCors = true,
    AllowedOrigins = new[] { "https://app.local" },
    AllowCredentials = true
});
```

For custom HTTP hosts, set CORS headers in the host layer.
