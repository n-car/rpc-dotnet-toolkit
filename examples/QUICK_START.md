# Quick Start Examples

Short examples for the core library. For detailed explanations, see the technical docs in `docs/`.

## In-Process Endpoint

```csharp
using RpcToolkit;

var endpoint = new RpcEndpoint();

endpoint.AddMethod<AddParams, int>("calculator.add", (p, ctx) =>
{
    return p!.A + p.B;
});

var request = @"{""jsonrpc"":""2.0"",""method"":""calculator.add"",""params"":{""a"":5,""b"":3},""id"":1}";
var response = await endpoint.HandleRequestAsync(request);

Console.WriteLine(response);

public sealed class AddParams
{
    public int A { get; set; }
    public int B { get; set; }
}
```

## C# HTTP Client

```csharp
using RpcToolkit;

using var client = new RpcClient("http://localhost:5000/rpc");

var result = await client.CallAsync<int>("calculator.add", new { a = 5, b = 3 });
Console.WriteLine(result);
```

## C# Client With Token

```csharp
using RpcToolkit;

using var client = new RpcClient("https://runtime.local/rpc");

client.SetAuthToken("admin-token");

var modules = await client.CallAsync<object>("modules.list");
```

## Custom HTTP Host

The HTTP host reads the request, builds a per-request context, then calls the endpoint.

```csharp
using System.Collections.Generic;
using System.Security.Claims;
using RpcToolkit;

var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
{
    new Claim(ClaimTypes.Name, "admin"),
    new Claim("scope", "modules.read modules.write")
}, "ApiKey"));

var headers = new Dictionary<string, string>
{
    ["X-Request-Id"] = "req-123"
};

var rpcContext = new RpcRequestContext(headers)
{
    Principal = principal,
    User = principal,
    RemoteIp = "127.0.0.1"
};

var responseJson = await endpoint.HandleRequestAsync(body, rpcContext);
```

The HTTP host should not parse the JSON-RPC method to decide authorization. Register policies on methods instead.

## Method Authorization

```csharp
endpoint.AddMethod<object, string>("modules.enable", (p, ctx) => "enabled", new MethodConfig
{
    RequiredScopes = new[] { "modules.write" }
});
```

Suggested policy mapping:

| Method | Required scope |
|--------|----------------|
| `tray.status` | `tray.status` |
| `modules.list` | `modules.read` |
| `modules.get` | `modules.read` |
| `modules.enable` | `modules.write` |
| `modules.disable` | `modules.write` |

## Browser Call

If the page is already served through an authenticated channel, let the browser use that channel.

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

The host should turn the existing session/cookie/Basic/API-key identity into a `ClaimsPrincipal` and put it into `RpcRequestContext`.

## Batch Requests

```csharp
var endpoint = new RpcEndpoint(null, new RpcOptions
{
    EnableBatch = true,
    BatchOptions = new RpcBatchOptions
    {
        MaxSize = 100,
        Parallel = true,
        MaxParallelism = 4
    }
});

endpoint.AddMethod<AddParams, int>("calculator.add", (p, ctx) => p!.A + p.B);

var batchRequest = @"[
  {""jsonrpc"":""2.0"",""method"":""calculator.add"",""params"":{""a"":1,""b"":2},""id"":1},
  {""jsonrpc"":""2.0"",""method"":""calculator.add"",""params"":{""a"":3,""b"":4},""id"":2}
]";

var response = await endpoint.HandleRequestAsync(batchRequest);
```

## Safe Mode

```csharp
using RpcToolkit;

var endpoint = new RpcSafeEndpoint();

using var client = new RpcSafeClient("http://localhost:5000/rpc");
```

Use safe endpoint and safe client together.

## Error Handling

```csharp
using RpcToolkit.Exceptions;

endpoint.AddMethod<int[], int>("math.divide", (p, ctx) =>
{
    if (p![1] == 0)
        throw new InvalidParamsException("Division by zero");

    return p[0] / p[1];
});
```
