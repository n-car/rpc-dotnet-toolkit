# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- N/A

### Changed
- N/A

### Deprecated
- N/A

### Removed
- N/A

### Fixed
- N/A

### Security
- N/A

## [1.1.3] - 2026-06-04

### Changed
- Updated embedded `RpcToolkit.AspNetCore` browser client assets to `rpc-toolkit-js-client` `v1.1.0`.

## [1.1.2] - 2026-06-04

### Added
- `RpcToolkit.AspNetCore` can now serve embedded shared JavaScript client assets with `UseRpcClientScripts()` or `MapRpcClientScripts()`.
- Browser bundles from `rpc-toolkit-js-client` are included in the ASP.NET Core package under `/vendor/rpc-client` by default.

## [1.1.1] - 2026-06-04

### Added
- `X-RPC-Safe-Enabled` request and response negotiation for .NET clients and ASP.NET Core hosted endpoints.

### Changed
- Safe Mode deserialization now normalizes prefixed values before typed conversion, improving interoperability with the shared JavaScript client.

### Fixed
- JSON-RPC envelope fields now serialize explicitly as `jsonrpc`, `method`, `params`, and `id` across serializers.
- `netstandard2.0` serialization now uses camelCase property names, matching the JavaScript toolkit and .NET 6+ behavior.
- The C# client now deserializes responses using the server safe-mode header when present.
- Batch requests now work when endpoint logging is disabled.

## [1.1.0] - 2026-06-04

### Added
- Per-request RPC context via `HandleRequestAsync(jsonRequest, context)`.
- `RpcRequestContext` for request headers, remote IP, principal, and platform-specific HTTP data.
- Method-level authorization with `MethodConfig.RequiredScopes`, `RequiredRoles`, `Authorize`, and `AuthorizeAsync`.
- `RpcAuthorizationContext` for custom authorization policies.
- `AuthorizationErrorException` and JSON-RPC authorization error code `-32004`.
- C# client auth helpers with `SetAuthToken(token, scheme)` and `ClearAuthToken()`.
- Technical documentation split into focused guides for clients, hosting, auth, Safe Mode, middleware, and examples.

### Changed
- ASP.NET Core middleware now passes per-request context, including headers and `HttpContext.User`, into the RPC endpoint.
- `AuthMiddleware` now supports existing authenticated principals in the context and fills `RpcRequestContext.Principal` when token auth returns a `ClaimsPrincipal`.
- README now acts as a short entry point instead of a full technical manual.

### Deprecated
- N/A

### Removed
- N/A

### Fixed
- Browser clients no longer need to provide a separate Bearer token when the HTTP host supplies an authenticated context.
- RPC authorization can now happen after JSON-RPC method lookup, removing the need for HTTP hosts to pre-parse method names.

### Security
- Added first-class separation between authentication and method-level authorization.

## [1.0.0] - TBD

Initial release with core features:
- JSON-RPC 2.0 compliance
- Server endpoint
- Client implementation
- Multi-targeting support
- Safe Mode serialization
- Cross-platform compatibility
