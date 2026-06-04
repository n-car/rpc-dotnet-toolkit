# HTTP Hosting

`RpcToolkit` does not require a specific HTTP server. The HTTP host is responsible for transport concerns:

- port and IP binding;
- HTTPS;
- route matching, such as `/rpc` and `/health`;
- HTTP headers;
- route-level authentication challenges, such as Basic Auth `401`;
- reading the request body and writing the response body.

The RPC endpoint is responsible for JSON-RPC parsing, method lookup, authorization, invocation, and JSON-RPC errors.

## Custom Host or HttpListener

For .NET Framework 4.8 or a custom runtime host, build a context per request.

```csharp
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
using RpcToolkit;

var principal = CreatePrincipalFromRequest(request);

var rpcContext = new RuntimeRpcRequestContext(request.Headers)
{
    RemoteIp = request.RemoteEndPoint?.Address?.ToString(),
    IsSecureConnection = request.IsSecureConnection,
    Principal = principal,
    User = principal
};

var responseJson = await rpcEndpoint.HandleRequestAsync(body, rpcContext);

public sealed class RuntimeRpcRequestContext : RpcRequestContext
{
    public RuntimeRpcRequestContext(NameValueCollection headers)
        : base(headers.AllKeys
            .Where(key => key != null)
            .ToDictionary(key => key!, key => headers[key!] ?? string.Empty))
    {
    }

    public bool IsSecureConnection { get; set; }
}
```

Do not parse the JSON-RPC method in the HTTP layer to decide authorization. Register method policies on the RPC method instead.

## ASP.NET Core

Install:

```bash
dotnet add package RpcToolkit.AspNetCore
```

Register an endpoint:

```csharp
using RpcToolkit;
using RpcToolkit.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRpcEndpoint(
    options: new RpcOptions
    {
        EnableBatch = true
    },
    configure: endpoint =>
    {
        endpoint.AddMethod<object, string>("system.ping", (p, ctx) => "pong");
    });

var app = builder.Build();

app.UseRpcClientScripts();

app.UseRpc(new RpcMiddlewareOptions
{
    Path = "/rpc",
    EnableCors = true,
    AllowedOrigins = new[] { "https://app.local" }
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
```

The ASP.NET Core adapter creates an `RpcRequestContext` from `HttpContext`, including headers and `HttpContext.User`.

## Browser Client Assets

`RpcToolkit.AspNetCore` embeds the shared `rpc-toolkit-js-client` browser bundles. Serve them with middleware:

```csharp
app.UseRpcClientScripts("/vendor/rpc-client");
```

Or with endpoint routing:

```csharp
app.MapRpcClientScripts();
```

Default files:

```text
/vendor/rpc-client/rpc-client.js
/vendor/rpc-client/rpc-client.min.js
/vendor/rpc-client/rpc-client.mjs
/vendor/rpc-client/rpc-client.min.mjs
```

Then load the global browser build:

```html
<script src="/vendor/rpc-client/rpc-client.min.js"></script>
<script>
  const client = new RpcToolkitClient.RpcClient("/rpc");
  const safeClient = new RpcToolkitClient.RpcSafeClient("/rpc");
</script>
```

Or the module build:

```html
<script type="module">
  import { RpcClient, RpcSafeClient } from "/vendor/rpc-client/rpc-client.mjs";

  const client = new RpcClient("/rpc");
  const safeClient = new RpcSafeClient("/rpc");
</script>
```

## Route-Level 401 vs JSON-RPC Errors

Use HTTP `401` when the route itself needs a transport challenge, for example browser Basic Auth.

Use JSON-RPC errors after request parsing:

- `AuthenticationErrorException`: no usable identity for a protected RPC method.
- `AuthorizationErrorException`: identity exists, but lacks scope, role, or custom policy permission.

For RPC clients, JSON-RPC errors are easier to handle because `RpcClient` maps them to typed exceptions.
