# Clients

There are two different client models.

## C# Client

Use `RpcClient` when a .NET process calls an RPC endpoint over HTTP.

```csharp
using RpcToolkit;

using var client = new RpcClient("http://localhost:5000/rpc");

var result = await client.CallAsync<int>("calculator.add", new { a = 5, b = 3 });
```

## C# Client Authentication

For service-to-service calls, set an authorization header explicitly.

```csharp
using var client = new RpcClient("https://runtime.local/rpc");

client.SetAuthToken("admin-token");
```

This sends:

```text
Authorization: Bearer admin-token
```

Use a different scheme when needed:

```csharp
client.SetAuthToken("base64-user-password", "Basic");
```

Clear it:

```csharp
client.ClearAuthToken();
```

You can also configure static custom headers:

```csharp
using var client = new RpcClient("https://runtime.local/rpc", new RpcClientOptions
{
    Headers =
    {
        ["X-Admin-Api-Key"] = "secret"
    }
});
```

## Batch Calls

```csharp
var responses = await client.BatchAsync(new[]
{
    new RpcRequest("calculator.add", new { a = 1, b = 2 }, "a"),
    new RpcRequest("calculator.add", new { a = 3, b = 4 }, "b")
});
```

## Notifications

```csharp
await client.NotifyAsync("log.write", new { message = "ready" });
```

Notifications do not expect a JSON-RPC result.

## Error Mapping

`RpcClient` converts JSON-RPC errors into typed exceptions.

```csharp
using RpcToolkit.Exceptions;

try
{
    await client.CallAsync<object>("modules.enable", new { id = "x" });
}
catch (AuthorizationErrorException)
{
    // Authenticated, but not allowed to call the method.
}
catch (AuthenticationErrorException)
{
    // Missing or invalid credentials.
}
```

## Browser Client

A browser client is different from the C# client. If the page is already served through an authenticated channel, do not add a second Bearer token unless your application explicitly needs it.

The shared JavaScript client lives in the separate `rpc-toolkit-js-client` project. ASP.NET Core hosts can serve the embedded browser bundles from `RpcToolkit.AspNetCore`:

```csharp
app.UseRpcClientScripts();
```

Default asset paths:

```text
/vendor/rpc-client/rpc-client.js
/vendor/rpc-client/rpc-client.min.js
/vendor/rpc-client/rpc-client.mjs
/vendor/rpc-client/rpc-client.min.mjs
```

Browser global build:

```html
<script src="/vendor/rpc-client/rpc-client.min.js"></script>
<script>
  (async () => {
    const client = new RpcToolkitClient.RpcClient("/rpc", {}, {
      fetchOptions: { credentials: "same-origin" }
    });

    const status = await client.call("tray.status");

    const safeClient = new RpcToolkitClient.RpcSafeClient("/rpc", {}, {
      fetchOptions: { credentials: "same-origin" }
    });

    await safeClient.notify("tray.opened");
  })();
</script>
```

Browser module build:

```html
<script type="module">
  import { RpcClient, RpcSafeClient } from "/vendor/rpc-client/rpc-client.mjs";

  const client = new RpcClient("/rpc", {}, {
    fetchOptions: { credentials: "same-origin" }
  });

  const status = await client.call("tray.status");

  const safeClient = new RpcSafeClient("/rpc", {}, {
    fetchOptions: { credentials: "same-origin" }
  });

  await safeClient.notify("tray.opened");
</script>
```

Typical browser behavior:

```javascript
const response = await fetch("/rpc", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  credentials: "same-origin",
  body: JSON.stringify({
    jsonrpc: "2.0",
    method: "tray.status",
    params: {},
    id: 1
  })
});

const rpcResponse = await response.json();
```

The HTTP host should build a `ClaimsPrincipal` from the existing channel: cookie, Basic Auth, TLS/client identity, reverse proxy identity, or application session. Then pass that principal into `RpcRequestContext`. Method authorization remains inside the RPC endpoint.

See [Authentication and Authorization](AUTHORIZATION.md).
