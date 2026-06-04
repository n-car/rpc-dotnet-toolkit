# Safe Mode

Safe Mode is a serialization preset for better round-tripping of values that are awkward in plain JSON.

Use `RpcSafeEndpoint` and `RpcSafeClient` when both sides agree to Safe Mode.

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
