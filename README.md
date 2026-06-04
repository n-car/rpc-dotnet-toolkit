# RPC .NET Toolkit

JSON-RPC 2.0 toolkit for .NET applications. It provides a server endpoint, typed C# client, batch requests, Safe Mode serialization, middleware, and method-level authorization.

## Packages

```bash
dotnet add package RpcToolkit
```

For ASP.NET Core hosting:

```bash
dotnet add package RpcToolkit.AspNetCore
```

## Target Frameworks

`RpcToolkit` targets:

- `netstandard2.0`, usable from .NET Framework 4.8 and modern .NET
- `net6.0`
- `net8.0`

`RpcToolkit.AspNetCore` targets ASP.NET Core on modern .NET.

## Minimal Endpoint

```csharp
using RpcToolkit;

var endpoint = new RpcEndpoint();

endpoint.AddMethod<AddParams, int>("calculator.add", (p, ctx) =>
{
    return p!.A + p.B;
});

var json = @"{""jsonrpc"":""2.0"",""method"":""calculator.add"",""params"":{""a"":5,""b"":3},""id"":1}";
var response = await endpoint.HandleRequestAsync(json);

public sealed class AddParams
{
    public int A { get; set; }
    public int B { get; set; }
}
```

## Minimal Client

```csharp
using RpcToolkit;

using var client = new RpcClient("http://localhost:5000/rpc");

var result = await client.CallAsync<int>("calculator.add", new { a = 5, b = 3 });
```

## Documentation

Start here:

- [Getting Started](docs/GETTING_STARTED.md): endpoint, methods, requests, errors.
- [Clients](docs/CLIENTS.md): C# client, browser client expectations, auth headers.
- [HTTP Hosting](docs/HTTP_HOSTING.md): ASP.NET Core and custom `HttpListener` hosts.
- [Authentication and Authorization](docs/AUTHORIZATION.md): request context, `ClaimsPrincipal`, scopes, roles, custom policies.
- [Safe Mode](docs/SAFE_MODE.md): `RpcSafeEndpoint`, `RpcSafeClient`, serialization behavior.
- [Middleware, Batch, Logging](docs/MIDDLEWARE_BATCH_LOGGING.md): operational features.

Examples:

- [Quick Start Examples](examples/QUICK_START.md)
- [Example Projects](examples/README.md)

## Core Concepts

- The HTTP host owns ports, paths, TLS, `/health`, CORS, and route-level challenges.
- `RpcEndpoint` and `RpcSafeEndpoint` parse JSON-RPC and invoke registered methods.
- Per-request data is passed with `HandleRequestAsync(json, context)`.
- Method authorization belongs in RPC method configuration, because the method is known only after parsing and lookup.

## Build

```bash
dotnet restore
dotnet build
dotnet test
```

## License

MIT. See [LICENSE](LICENSE).
