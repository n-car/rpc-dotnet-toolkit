# Getting Started

This guide shows the core library without any HTTP framework. Use it when you want to embed JSON-RPC in a custom host, a test, or a .NET Framework 4.8 application.

## Create an Endpoint

```csharp
using RpcToolkit;

var endpoint = new RpcEndpoint();

endpoint.AddMethod<AddParams, int>("calculator.add", (p, ctx) =>
{
    return p!.A + p.B;
});

public sealed class AddParams
{
    public int A { get; set; }
    public int B { get; set; }
}
```

Method handlers receive:

- typed params, deserialized from JSON-RPC `params`;
- a context object, supplied when handling the request.

## Handle a Request

```csharp
var requestJson = @"{
  ""jsonrpc"": ""2.0"",
  ""method"": ""calculator.add"",
  ""params"": { ""a"": 5, ""b"": 3 },
  ""id"": 1
}";

var responseJson = await endpoint.HandleRequestAsync(requestJson);
```

Response:

```json
{"jsonrpc":"2.0","result":8,"id":1}
```

## Register Methods With Metadata

```csharp
endpoint.AddMethod<AddParams, int>("calculator.add", (p, ctx) => p!.A + p.B, new MethodConfig
{
    Description = "Add two numbers",
    ExposeSchema = true,
    Schema = new
    {
        type = "object",
        properties = new
        {
            a = new { type = "number" },
            b = new { type = "number" }
        },
        required = new[] { "a", "b" }
    }
});
```

## Use a Per-Request Context

A context can contain services, request headers, remote IP, a principal, or application-specific values.

```csharp
var context = new RpcRequestContext(new Dictionary<string, string>
{
    ["X-Request-Id"] = "req-123"
})
{
    RemoteIp = "127.0.0.1"
};

var responseJson = await endpoint.HandleRequestAsync(requestJson, context);
```

Inside a handler:

```csharp
endpoint.AddMethod<object, string>("request.ip", (p, ctx) =>
{
    var requestContext = (RpcRequestContext)ctx!;
    return requestContext.RemoteIp ?? "unknown";
});
```

## Notifications

JSON-RPC notifications omit `id`. The endpoint invokes the handler and returns an empty response body.

```json
{"jsonrpc":"2.0","method":"log.write","params":{"message":"ready"}}
```

## Batch Requests

```csharp
var batchJson = @"[
  {""jsonrpc"":""2.0"",""method"":""calculator.add"",""params"":{""a"":1,""b"":2},""id"":1},
  {""jsonrpc"":""2.0"",""method"":""calculator.add"",""params"":{""a"":3,""b"":4},""id"":2}
]";

var responseJson = await endpoint.HandleRequestAsync(batchJson);
```

Configure batch behavior with `RpcOptions.BatchOptions`.

```csharp
var endpoint = new RpcEndpoint(null, new RpcOptions
{
    EnableBatch = true,
    BatchOptions = new RpcBatchOptions
    {
        MaxSize = 50,
        Parallel = true,
        MaxParallelism = 4,
        ContinueOnError = true
    }
});
```

## Error Handling

Throw RPC exceptions from handlers when you want a JSON-RPC error response.

```csharp
using RpcToolkit.Exceptions;

endpoint.AddMethod<int[], int>("math.divide", (p, ctx) =>
{
    if (p![1] == 0)
        throw new InvalidParamsException("Division by zero");

    return p[0] / p[1];
});
```

Common exceptions:

- `InvalidRequestException`
- `MethodNotFoundException`
- `InvalidParamsException`
- `AuthenticationErrorException`
- `AuthorizationErrorException`
- `RateLimitExceededException`
- `ValidationErrorException`
