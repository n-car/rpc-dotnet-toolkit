# RPC Server Example

Runnable ASP.NET Core server using `RpcToolkit.AspNetCore`.

## What It Shows

- ASP.NET Core hosting at `/rpc`
- health endpoint at `/health`
- browser client assets at `/vendor/rpc-client`
- calculator methods
- user methods
- batch requests
- rate limiting middleware
- method metadata and introspection

This example intentionally keeps RPC methods open so it is easy to run with curl. For authentication and method authorization, see [Authentication and Authorization](../../docs/AUTHORIZATION.md).

## Run

```bash
cd examples/RpcServer.Example
dotnet run
```

The server listens on the URLs configured by ASP.NET Core. The RPC path is `/rpc`.
The browser client is served from `/vendor/rpc-client/rpc-client.min.js`.

## Browser Client

Classic browser script:

```html
<script src="/vendor/rpc-client/rpc-client.min.js"></script>
<script>
  const client = new RpcToolkitClient.RpcClient("/rpc");
  const safeClient = new RpcToolkitClient.RpcSafeClient("/rpc");

  client.call("system.ping").then(console.log);
  safeClient.notify("system.ping");
</script>
```

Module script:

```html
<script type="module">
  import { RpcClient, RpcSafeClient } from "/vendor/rpc-client/rpc-client.mjs";

  const client = new RpcClient("/rpc");
  const safeClient = new RpcSafeClient("/rpc");

  console.log(await client.call("system.ping"));
  await safeClient.notify("system.ping");
</script>
```

## Ping

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"system.ping","params":{},"id":1}'
```

Response:

```json
{"jsonrpc":"2.0","result":"pong","id":1}
```

## Calculator

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"calculator.add","params":{"a":5,"b":3},"id":2}'
```

## Users

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"user.list","params":{},"id":3}'
```

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"user.create","params":{"name":"Bob","email":"bob@example.com"},"id":4}'
```

## Batch

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '[
    {"jsonrpc":"2.0","method":"calculator.add","params":{"a":1,"b":2},"id":"calc1"},
    {"jsonrpc":"2.0","method":"calculator.multiply","params":{"a":3,"b":4},"id":"calc2"},
    {"jsonrpc":"2.0","method":"system.version","params":{},"id":"sys1"}
  ]'
```

## Introspection

The example enables introspection under `__rpc`.

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"__rpc.listMethods","params":{},"id":5}'
```

## Available Methods

| Method | Description |
|--------|-------------|
| `calculator.add` | Add two numbers |
| `calculator.subtract` | Subtract two numbers |
| `calculator.multiply` | Multiply two numbers |
| `calculator.divide` | Divide two numbers |
| `user.get` | Get user by ID |
| `user.create` | Create a user |
| `user.list` | List users |
| `system.version` | Get server version |
| `system.ping` | Health check |
| `system.time` | Get UTC time |
