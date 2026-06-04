# Examples

This directory contains small examples and one runnable ASP.NET Core server.

## Quick Snippets

Read [QUICK_START.md](QUICK_START.md) for compact examples of:

- endpoint registration;
- C# client calls;
- per-request context;
- scopes and method authorization;
- browser calls through an existing authenticated channel;
- batch requests;
- Safe Mode.

## Runnable Server

`RpcServer.Example` is an ASP.NET Core server with calculator, user, system, batch, middleware, and introspection examples.

```bash
cd examples/RpcServer.Example
dotnet run
```

Then call:

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"system.ping","params":{},"id":1}'
```

## Safe Mode Console Example

`SafeModeExample.cs` is a standalone source example showing `RpcSafeEndpoint` and `RpcSafeClient`.

## Related Documentation

- [Getting Started](../docs/GETTING_STARTED.md)
- [Clients](../docs/CLIENTS.md)
- [HTTP Hosting](../docs/HTTP_HOSTING.md)
- [Authentication and Authorization](../docs/AUTHORIZATION.md)
- [Safe Mode](../docs/SAFE_MODE.md)
