# RPC .NET Toolkit - Project Plan

## 📋 Project Structure

```
rpc-dotnet-toolkit/
├── src/
│   ├── RpcToolkit/                      # Core library
│   │   ├── RpcToolkit.csproj           # Multi-targeting project
│   │   ├── RpcEndpoint.cs              # Server endpoint
│   │   ├── RpcClient.cs                # Client implementation
│   │   ├── RpcTypes.cs                 # Common types (Request, Response, Options)
│   │   ├── Serialization/
│   │   │   ├── SerializerFactory.cs    # Conditional serializer selection
│   │   │   ├── SafeSerializer.cs       # Safe mode serialization
│   │   │   └── TypeConverters.cs       # BigInteger, DateTime converters
│   │   ├── Middleware/
│   │   │   ├── IMiddleware.cs
│   │   │   ├── MiddlewareManager.cs
│   │   │   ├── CorsMiddleware.cs
│   │   │   ├── RateLimitMiddleware.cs
│   │   │   └── AuthMiddleware.cs
│   │   ├── Validation/
│   │   │   └── SchemaValidator.cs
│   │   ├── Logging/
│   │   │   └── RpcLogger.cs
│   │   ├── Batch/
│   │   │   └── BatchHandler.cs
│   │   └── Exceptions/
│   │       ├── RpcException.cs
│   │       ├── MethodNotFoundException.cs
│   │       ├── InvalidParamsException.cs
│   │       ├── InvalidRequestException.cs
│   │       └── InternalErrorException.cs
│   │
│   └── RpcToolkit.AspNetCore/          # ASP.NET Core integration
│       ├── RpcToolkit.AspNetCore.csproj
│       ├── RpcMiddleware.cs
│       └── RpcExtensions.cs
│
├── tests/
│   ├── RpcToolkit.Tests/
│   │   ├── RpcToolkit.Tests.csproj
│   │   ├── RpcEndpointTests.cs
│   │   ├── RpcClientTests.cs
│   │   ├── SerializationTests.cs
│   │   ├── MiddlewareTests.cs
│   │   └── ValidationTests.cs
│   │
│   └── RpcToolkit.Integration.Tests/
│       ├── RpcToolkit.Integration.Tests.csproj
│       ├── CrossPlatformTests.cs       # Test with Node/PHP
│       └── PerformanceTests.cs
│
├── examples/
│   ├── AspNetCore.Server/
│   ├── ConsoleClient/
│   ├── BlazorApp/
│   └── CrossPlatform/
│
├── docs/
│   ├── getting-started.md
│   ├── api-reference.md
│   └── migration-guide.md
│
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── release.yml
│
├── README.md
├── CHANGELOG.md
├── LICENSE
└── rpc-dotnet-toolkit.sln
```

## 🎯 Implementation Phases

### Phase 1: Core Foundation (Week 1)

**Day 1-2: Project Setup**
- [x] Create solution structure
- [x] Setup multi-targeting (.NET Standard 2.0, .NET 6, .NET 8)
- [x] Configure NuGet package metadata
- [x] Basic types (RpcRequest, RpcResponse, RpcError)
- [ ] Conditional compilation setup

**Day 3-4: Serialization**
- [ ] SerializerFactory (switches between Newtonsoft/System.Text.Json)
- [ ] SafeSerializer implementation
- [ ] BigInteger support
- [ ] DateTime/DateTimeOffset support
- [ ] Tests for serialization

**Day 5-7: Core RPC**
- [ ] RpcEndpoint basic implementation
- [ ] Method registration
- [ ] Request handling
- [ ] Error handling
- [ ] RpcClient basic implementation
- [ ] Unit tests

### Phase 2: Advanced Features (Week 2)

**Day 8-10: Middleware System**
- [ ] IMiddleware interface
- [ ] MiddlewareManager
- [ ] CorsMiddleware
- [ ] RateLimitMiddleware
- [ ] AuthMiddleware
- [ ] Tests

**Day 11-12: Validation & Batch**
- [ ] SchemaValidator with NJsonSchema
- [ ] BatchHandler
- [ ] Tests

**Day 13-14: ASP.NET Core Integration**
- [ ] RpcToolkit.AspNetCore project
- [ ] Middleware extension methods
- [ ] Dependency injection support
- [ ] Example server

### Phase 3: Polish & Documentation (Week 3)

**Day 15-16: Examples**
- [ ] ASP.NET Core server example
- [ ] Console client example
- [ ] Blazor app example
- [ ] Cross-platform examples (Node/PHP)

**Day 17-18: Documentation**
- [ ] API documentation
- [ ] Getting started guide
- [ ] Migration guide
- [ ] Performance benchmarks

**Day 19-21: Testing & CI/CD**
- [ ] Integration tests
- [ ] Performance tests
- [ ] GitHub Actions CI
- [ ] Code coverage

### Phase 4: Release (Week 4)

**Day 22-24: Pre-Release**
- [ ] NuGet package testing
- [ ] Documentation review
- [ ] Breaking change review
- [ ] Version 1.0.0-beta

**Day 25-28: Release**
- [ ] Community feedback
- [ ] Bug fixes
- [ ] Final documentation
- [ ] Version 1.0.0 release

## 🔧 Technical Decisions

### Serialization Strategy

```csharp
#if NETSTANDARD2_0
    // Use Newtonsoft.Json for .NET Standard 2.0
    using Newtonsoft.Json;
#else
    // Use System.Text.Json for .NET 6+
    using System.Text.Json;
#endif
```

### Async Strategy

```csharp
#if NETSTANDARD2_0
    // Use Task<T> everywhere
    public async Task<object> HandleAsync(...)
#else
    // Use ValueTask<T> for hot paths
    public async ValueTask<object> HandleAsync(...)
#endif
```

### Performance Optimizations (.NET 6+ only)

- Span<T> for zero-allocation parsing
- ArrayPool for buffer reuse
- Channels for batch processing
- IAsyncEnumerable for streaming

## 📊 Success Criteria

- [ ] All tests passing on all target frameworks
- [ ] Code coverage > 80%
- [ ] NuGet package published
- [ ] Documentation complete
- [ ] Cross-platform compatibility verified
- [ ] Performance benchmarks published
- [ ] Example projects working

## 🚀 Post-Release

### v1.1.0
- OpenTelemetry integration
- Health check endpoints
- Metrics collection

### v1.2.0
- gRPC transport option
- WebSocket support
- HTTP/3 support (.NET 8)

### v2.0.0 (Breaking Changes)
- Drop .NET Standard 2.0 support
- Modern C# features only
- Performance-first design

## 📝 Notes

- Keep feature parity with rpc-express-toolkit and rpc-php-toolkit
- Prioritize correctness over performance initially
- Add performance optimizations in .NET 6+ without breaking .NET Standard 2.0
- Document all breaking changes clearly
- Use semantic versioning strictly
