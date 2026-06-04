# Authentication and Authorization

Authentication identifies the caller. Authorization decides whether that caller can invoke a JSON-RPC method.

Keep these responsibilities separate:

- HTTP host: builds the per-request identity and transport context.
- RPC endpoint: authorizes the parsed JSON-RPC method.

## Request Context

Pass per-request data through `RpcRequestContext`.

```csharp
using System.Security.Claims;
using RpcToolkit;

var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
{
    new Claim(ClaimTypes.Name, "admin"),
    new Claim("scope", "runtime.admin"),
    new Claim("scope", "modules.read modules.write"),
    new Claim("scope", "tray.status")
}, "ApiKey"));

var context = new RpcRequestContext(headers)
{
    Principal = principal,
    User = principal,
    RemoteIp = "127.0.0.1"
};

var responseJson = await endpoint.HandleRequestAsync(body, context);
```

The context can come from Basic Auth, API key, tray key, cookie, Windows identity, or another application-specific channel.

## Scope-Based Authorization

Declare method policies where methods are registered.

```csharp
endpoint.AddMethod<object, string>("modules.enable", (p, ctx) => "enabled", new MethodConfig
{
    RequiredScopes = new[] { "modules.write" }
});
```

If no principal is present, the endpoint returns an authentication error. If a principal is present but lacks the scope, it returns an authorization error.

## Role-Based Authorization

```csharp
endpoint.AddMethod<object, string>("runtime.shutdown", (p, ctx) => "ok", new MethodConfig
{
    RequiredRoles = new[] { "Admin" }
});
```

`RequiredRoles` allows any listed role.

## Custom Policies

Use `Authorize` or `AuthorizeAsync` when scopes and roles are not enough.

```csharp
endpoint.AddMethod<object, string>("tray.status", (p, ctx) => "online", new MethodConfig
{
    AuthorizeAsync = auth =>
    {
        var principal = auth.Principal;
        var requestContext = auth.Context;

        var allowed = principal?.HasClaim("scope", "tray.status") == true &&
                      requestContext?.RemoteIp == "127.0.0.1";

        return Task.FromResult(allowed);
    }
});
```

`RpcAuthorizationContext` contains:

- `Request`: the parsed JSON-RPC request.
- `Method`: the registered `MethodConfig`.
- `Context`: the typed `RpcRequestContext`, when available.
- `RawContext`: the original context object.
- `Principal`: the authenticated `ClaimsPrincipal`, when available.

## Suggested Runtime Scopes

Example policy model:

| Caller | Claims |
|--------|--------|
| Admin | `runtime.admin`, `modules.read`, `modules.write`, `tray.status` |
| Tray | `tray.status` |

Example method policies:

| Method | Required scope |
|--------|----------------|
| `tray.status` | `tray.status` |
| `modules.list` | `modules.read` |
| `modules.get` | `modules.read` |
| `modules.enable` | `modules.write` |
| `modules.disable` | `modules.write` |

Keep UI permissions separate from RPC scopes. For example `OpenAdminPage` or `ShowBalloon` can be returned by `tray.status` as application payload values; they do not need to be RPC authorization scopes.

## Token Authentication Middleware

`AuthMiddleware` is useful when a client sends `Authorization` or another token-like header and you want to populate the request context.

```csharp
using RpcToolkit.Middleware;

endpoint.GetMiddleware()?.Add(new AuthMiddleware(token =>
{
    if (token == "tray-secret")
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "tray"),
            new Claim("scope", "tray.status")
        }, "TrayKey"));
    }

    return null;
}), "before");
```

If the context already contains an authenticated principal, `AuthMiddleware` uses it and does not require a token.

## Error Codes

| Exception | Code | Meaning |
|-----------|------|---------|
| `AuthenticationErrorException` | `-32001` | Missing or invalid identity |
| `AuthorizationErrorException` | `-32004` | Identity exists but method policy denied access |
