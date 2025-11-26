# RPC .NET Toolkit - Quick Start Examples

## Basic Server Example

```csharp
using System;
using System.Threading.Tasks;
using RpcToolkit;

class Program
{
    static async Task Main()
    {
        // Create endpoint
        var endpoint = new RpcEndpoint();

        // Register methods
        endpoint.AddMethod<AddParams, int>("add", (params, ctx) =>
        {
            return params.A + params.B;
        });

        endpoint.AddMethod<EchoParams, string>("echo", (params, ctx) =>
        {
            return $"Echo: {params.Message}";
        });

        // Handle JSON-RPC request
        var request = @"{
            ""jsonrpc"": ""2.0"",
            ""method"": ""add"",
            ""params"": { ""a"": 5, ""b"": 3 },
            ""id"": 1
        }";

        var response = await endpoint.HandleRequestAsync(request);
        Console.WriteLine(response);
        // Output: {"jsonrpc":"2.0","result":8,"id":1}
    }
}

public class AddParams
{
    public int A { get; set; }
    public int B { get; set; }
}

public class EchoParams
{
    public string Message { get; set; }
}
```

## Client Example

```csharp
using System;
using System.Threading.Tasks;
using RpcToolkit;

class Program
{
    static async Task Main()
    {
        // Create client
        using var client = new RpcClient("http://localhost:5000/rpc");

        // Single call
        var result = await client.CallAsync<int>("add", new { a = 5, b = 3 });
        Console.WriteLine($"Result: {result}"); // 8

        // Batch request
        var batch = await client.BatchAsync(new[]
        {
            new RpcRequest("add", new { a = 1, b = 2 }, "req1"),
            new RpcRequest("add", new { a = 3, b = 4 }, "req2")
        });

        foreach (var response in batch)
        {
            Console.WriteLine($"ID {response.Id}: {response.Result}");
        }

        // Notification (no response)
        await client.NotifyAsync("log", new { message = "Hello" });
    }
}
```

## Middleware Example

```csharp
using RpcToolkit;
using RpcToolkit.Middleware;

var endpoint = new RpcEndpoint();

// Add CORS middleware
endpoint.GetMiddleware()?.Add(new CorsMiddleware(new CorsOptions
{
    AllowedOrigins = new[] { "http://localhost:3000" },
    AllowCredentials = true
}), "before");

// Add rate limiting
endpoint.GetMiddleware()?.Add(
    new RateLimitMiddleware(100, 60, "ip"), 
    "before"
);

// Add authentication
endpoint.GetMiddleware()?.Add(new AuthMiddleware(
    token => {
        // Validate token and return user
        return token == "secret" ? new { UserId = 123 } : null;
    },
    allowedMethods: new[] { "ping", "version" },
    required: true
), "before");

// Register methods
endpoint.AddMethod<object, string>("ping", (p, ctx) => "pong");
endpoint.AddMethod<object, string>("protected", (p, ctx) => "Protected data");
```

## Safe Mode Example

```csharp
using RpcToolkit;

// Enable safe serialization
var options = new RpcOptions
{
    SafeEnabled = true
};

var endpoint = new RpcEndpoint(null, options);

// BigInt and DateTime will be serialized with prefixes
endpoint.AddMethod<object, object>("getData", (p, ctx) => new
{
    Timestamp = DateTime.UtcNow,        // Serialized as "D:2025-11-26T..."
    LargeNumber = 9007199254740992L     // Serialized as "9007199254740992n"
});
```

## Batch Processing

```csharp
var endpoint = new RpcEndpoint(null, new RpcOptions
{
    EnableBatch = true,
    MaxBatchSize = 100
});

endpoint.AddMethod<AddParams, int>("add", (p, ctx) => p.A + p.B);

var batchRequest = @"[
    {""jsonrpc"":""2.0"",""method"":""add"",""params"":{""a"":1,""b"":2},""id"":1},
    {""jsonrpc"":""2.0"",""method"":""add"",""params"":{""a"":3,""b"":4},""id"":2},
    {""jsonrpc"":""2.0"",""method"":""add"",""params"":{""a"":5,""b"":6},""id"":3}
]";

var response = await endpoint.HandleRequestAsync(batchRequest);
// Returns array of results: [{"jsonrpc":"2.0","result":3,"id":1}, ...]
```

## Context Example

```csharp
var contextData = new
{
    Database = dbConnection,
    Config = configuration,
    UserId = 123
};

var endpoint = new RpcEndpoint(contextData);

endpoint.AddMethod<object, string>("getUserData", (params, context) =>
{
    // Access context
    dynamic ctx = context;
    var userId = ctx.UserId;
    var db = ctx.Database;
    
    // Use context in method logic
    return $"Data for user {userId}";
});
```

## Error Handling

```csharp
using RpcToolkit.Exceptions;

var endpoint = new RpcEndpoint();

endpoint.AddMethod<int[], int>("divide", (params, ctx) =>
{
    if (params[1] == 0)
    {
        throw new InvalidParamsException("Division by zero", new { denominator = 0 });
    }
    return params[0] / params[1];
});

// Invalid request returns error:
// {"jsonrpc":"2.0","error":{"code":-32602,"message":"Division by zero","data":{"denominator":0}},"id":1}
```
