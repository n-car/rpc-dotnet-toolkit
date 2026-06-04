# RPC .NET Toolkit - Project Plan

Status updated: 2026-06-04

This document is the active project status and roadmap. Older phase checklists were replaced because the implementation has moved beyond the original bootstrap plan.

## Current Releases

- `RpcToolkit`: `1.1.1`
- `RpcToolkit.AspNetCore`: `1.1.2-beta.1`
- Shared browser/Node client: `rpc-toolkit-js-client` `1.0.0`
- Express adapter compatibility target: `rpc-express-toolkit` `4.2.3`

## Current Architecture

```text
rpc-dotnet-toolkit/
├── src/
│   ├── RpcToolkit/                    # Core endpoint, C# client, Safe Mode, batch, auth, middleware
│   └── RpcToolkit.AspNetCore/         # ASP.NET Core middleware and embedded browser client assets
├── tests/RpcToolkit.Tests/            # Unit and behavioral tests
├── examples/
│   ├── RpcServer.Example/             # Runnable ASP.NET Core server
│   └── SafeModeExample.cs
├── docs/                              # Focused technical guides
└── .github/workflows/ci.yml           # Build/test/pack workflow
```

## Implemented

- Multi-targeting for `netstandard2.0`, `net6.0`, and `net8.0`.
- JSON-RPC 2.0 request/response models with explicit wire names.
- `RpcEndpoint` and `RpcSafeEndpoint`.
- Typed C# `RpcClient` and `RpcSafeClient`.
- Conditional serialization:
  - Newtonsoft.Json for `netstandard2.0`;
  - System.Text.Json for modern .NET.
- Safe Mode type handling for string/date/big integer round-trips.
- Batch requests with configurable batch options.
- Middleware system, rate limiting, CORS, auth middleware, logging.
- Schema validation with NJsonSchema.
- Per-request `RpcRequestContext`.
- Method-level authorization through scopes, roles, sync policies, and async policies.
- ASP.NET Core middleware that passes `HttpContext` data into the RPC context.
- ASP.NET Core browser asset hosting with:
  - `UseRpcClientScripts()`;
  - `MapRpcClientScripts()`.
- Documentation split into focused guides:
  - getting started;
  - clients;
  - HTTP hosting;
  - authentication and authorization;
  - Safe Mode;
  - middleware, batch, logging.
- CI workflow for restore, build, test, coverage upload, and package artifact creation.

## Verified Locally

Latest local verification:

```bash
dotnet restore rpc-dotnet-toolkit.sln
dotnet build src/RpcToolkit.AspNetCore/RpcToolkit.AspNetCore.csproj -f net6.0 --no-restore
dotnet build src/RpcToolkit.AspNetCore/RpcToolkit.AspNetCore.csproj -f net8.0 --no-restore
DOTNET_ROLL_FORWARD=Major dotnet test tests/RpcToolkit.Tests/RpcToolkit.Tests.csproj --no-restore
dotnet build examples/RpcServer.Example/RpcServer.Example.csproj --no-restore
```

Also verified manually:

- `/vendor/rpc-client/rpc-client.min.js` returns `200 OK` from the ASP.NET Core example.
- `/vendor/rpc-client/rpc-client.mjs` returns `200 OK` from the ASP.NET Core example.
- `/rpc` responds correctly to `system.ping`.
- Shared JavaScript client interoperability was tested against .NET and Express endpoints in standard and Safe Mode paths.

## Open Gaps

### Package and Release

- Publish workflow is not automated end-to-end.
- NuGet publishing is not wired to tags/releases.
- Package artifact smoke tests should be added before public publishing.
- `RpcToolkit.AspNetCore` is still beta-versioned while the core package is stable.

### Examples

- Add a .NET Framework 4.8 custom host / `HttpListener` example.
- Add a console client example.
- Add a browser page example that consumes `UseRpcClientScripts()`.
- Add a cross-repo interoperability example using `rpc-toolkit-js-client`.
- Blazor remains optional and should not block current release work.

### Tests

- Add integration tests for ASP.NET Core middleware and embedded JS assets.
- Add package-level tests using produced `.nupkg` artifacts.
- Add cross-repository compatibility tests against:
  - `rpc-toolkit-js-client`;
  - `rpc-express-toolkit`.
- Add CI coverage threshold reporting if the project wants to enforce a numeric target.

### Documentation

- Add API reference generated from XML docs or a focused manual API guide.
- Add migration guide for older RPC implementations.
- Add release/publish documentation for maintainers.

### Performance

- Performance benchmarks are not implemented.
- No zero-allocation parsing or ArrayPool optimization has been attempted yet.
- Performance work should stay behind correctness and compatibility.

## Priority Roadmap

### P0 - Release Readiness

1. Add package smoke tests:
   - pack `RpcToolkit`;
   - pack `RpcToolkit.AspNetCore`;
   - install into a temporary consumer project;
   - verify basic client/server compile.
2. Decide versioning policy for `RpcToolkit.AspNetCore`:
   - keep beta until more ASP.NET integration tests exist;
   - or promote when asset hosting and middleware tests are in CI.
3. Add maintainer release notes:
   - tag format;
   - NuGet push steps;
   - rollback steps.

### P1 - Compatibility Harness

1. Add a local integration harness that runs:
   - .NET endpoint + shared JS client;
   - Express endpoint + .NET client;
   - Safe Mode enabled/disabled.
2. Keep the harness deterministic and runnable without publishing packages.
3. Promote it into CI after runtime setup is stable.

### P1 - Runtime Examples

1. Add `.NET Framework 4.8` host example focused on:
   - `RpcRequestContext`;
   - Basic/API-key authentication at route level;
   - method authorization inside `RpcEndpoint`.
2. Add browser example using:
   - `/vendor/rpc-client/rpc-client.min.js`;
   - existing authenticated channel;
   - no duplicate Bearer token by default.

### P2 - Documentation Completion

1. Add API reference.
2. Add migration guide.
3. Add troubleshooting guide for:
   - Safe Mode header mismatches;
   - serializer differences;
   - ASP.NET routing/CORS issues.

### P2 - Performance and Observability

1. Add BenchmarkDotNet project.
2. Add OpenTelemetry integration if needed by consuming runtimes.
3. Add health-check helpers only if they provide value beyond the host's existing endpoints.

## Cross-Repository Notes

- Browser/Node client ownership is now `rpc-toolkit-js-client`.
- `rpc-express-toolkit` should not maintain duplicated browser client source.
- `rpc-dotnet-toolkit` embeds built browser assets from `rpc-toolkit-js-client` for ASP.NET Core convenience.
- Protocol compatibility work should be validated against both .NET and Express before release.

## Deferred Ideas

- WebSocket transport.
- gRPC transport.
- HTTP/3-specific transport features.
- Dropping `netstandard2.0` support in a future major version.
