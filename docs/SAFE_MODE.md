# Safe Mode

Safe Mode is a serialization preset for better round-tripping of values that are awkward in plain JSON.

Use `RpcSafeEndpoint` and `RpcSafeClient` when both sides agree to Safe Mode.

Safe Mode is serialization, not authentication or encryption. Only `params`, `result`, and `error.data` are transformed. The JSON-RPC envelope stays unchanged: method names, IDs, protocol versions, error codes, and error messages are not prefixed or decoded. For example, an ID of `"S:request"` or `"123n"` remains that exact string.

## Server

```csharp
using RpcToolkit;

var endpoint = new RpcSafeEndpoint();

endpoint.AddMethod<object, object>("system.time", (p, ctx) => new
{
    Now = DateTime.UtcNow
});
```

## Client

```csharp
using RpcToolkit;

using var client = new RpcSafeClient("https://localhost:5001/rpc");

var value = await client.CallAsync<object>("system.time");
```

## Manual Configuration

```csharp
var endpoint = new RpcEndpoint(null, new RpcOptions
{
    SafeEnabled = true
});

using var client = new RpcClient("https://localhost:5001/rpc", new RpcClientOptions
{
    SafeEnabled = true
});
```

Use the safe client and safe endpoint together. Mixing safe and non-safe serialization can produce unexpected values.

HTTP clients send `X-RPC-Safe-Enabled`; the ASP.NET Core adapter advertises the server mode in the same response header and permits/exposes it through its configured CORS policy. With `StrictMode = true`, a Safe Mode endpoint requires the negotiation header when an HTTP request context is available. Custom hosts must provide that context and manage their own headers. Notification, explicit null-ID, null-result, and invalid-envelope handling are identical in standard and Safe Mode.
