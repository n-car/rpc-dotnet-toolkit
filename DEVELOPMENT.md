# RPC .NET Toolkit - Development Notes

## 🏗️ Architecture

### Multi-Targeting Strategy

The project targets three frameworks:

- **.NET Standard 2.0** - Broad compatibility, including compatible Unity/Xamarin runtimes and .NET Framework applications; .NET Framework 4.8 is the documented custom-host target.
- **.NET 6.0** - Retained compatibility target.
- **.NET 8.0** - Development and test target.

### Conditional Compilation

Use `#if` to compile different code for each framework:

```csharp
#if NETSTANDARD2_0
    // Code for .NET Standard 2.0
    using Newtonsoft.Json;
#else
    // Code for .NET 6+ (.NET 6.0 and 8.0)
    using System.Text.Json;
#endif
```

Alternatively, use `NET6_0_OR_GREATER`:

```csharp
#if NET6_0_OR_GREATER
    // .NET 6+ only
    public async ValueTask<object> HandleAsync(...)
#else
    // .NET Standard 2.0
    public async Task<object> HandleAsync(...)
#endif
```

### Serialization

| Framework | Serializer | Relative performance goal | Notes |
|-----------|------------|-------------|------|
| netstandard2.0 | Newtonsoft.Json | ⭐⭐⭐ | Compatibility-first |
| net6.0 | System.Text.Json | ⭐⭐⭐⭐ | Modern + Fast |
| net8.0 | System.Text.Json | ⭐⭐⭐⭐⭐ | Modern runtime optimizations |

`SerializerFactory` selects the appropriate serializer at compile time. The ratings above are design goals, not measured benchmark results.

### Safe Mode

Safe Mode serialization adds type markers:

- **Strings**: `"hello"` → `"S:hello"`
- **Dates**: `DateTime` → `"D:2025-11-26T10:30:00Z"`
- **BigInteger**: `9007199254740992` → `"9007199254740992n"`

This distinguishes literal strings from numeric/date values in cross-platform RPC calls. Transformations apply only to `params`, `result`, and `error.data`; envelope fields, including IDs and error messages, remain unchanged. Safe Mode does not provide authentication or encryption.

The server validates the raw envelope before typed conversion. Omitted IDs identify notifications; explicit null IDs require responses. Both serializers preserve required null response fields, and invalid batch elements produce independent errors. See [Getting Started](docs/GETTING_STARTED.md) for the complete wire contract.

## 🔧 Development Setup

### Requirements

- .NET 8.0 SDK (for development and testing)
- Visual Studio 2022 / VS Code / Rider

### Build

```bash
# Restore dependencies
dotnet restore

# Build all frameworks
dotnet build

# Build a specific core-library target
dotnet build src/RpcToolkit/RpcToolkit.csproj -f netstandard2.0
dotnet build src/RpcToolkit/RpcToolkit.csproj -f net6.0
dotnet build src/RpcToolkit/RpcToolkit.csproj -f net8.0
```

### Test

```bash
# Run tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Package

The package version is 1.2.1. Packing is local; run the publishing commands only after release approval.

```bash
# Create NuGet package
dotnet pack -c Release -o ./nupkg

# Publish to local feed
dotnet nuget push ./nupkg/RpcToolkit.1.2.1.nupkg -s ~/nuget-local

# Publish to NuGet.org
dotnet nuget push ./nupkg/RpcToolkit.1.2.1.nupkg -s https://api.nuget.org/v3/index.json -k YOUR_API_KEY
```

## 📊 Performance Targets

### Benchmarks Goal (.NET 8 vs .NET Standard 2.0)

| Operation | Target Improvement |
|-----------|-------------------|
| Simple RPC call | 2.5x faster |
| Batch 100 items | 2.8x faster |
| Serialization | 3.0x faster |
| Memory allocation | 40% less |

### Candidate Optimization Techniques (.NET 6+)

These are optimization ideas, not claims about the current implementation.

1. **Span<T>** for zero-allocation parsing
2. **ArrayPool** for buffer reuse
3. **ValueTask<T>** for hot paths
4. **System.Text.Json** built-in
5. **ReadOnlySpan** for string operations

## 🧪 Testing Strategy

### Unit Tests

- `SerializationTests.cs` - Safe mode, round-trip
- `RpcEndpointTests.cs` - Method registration, handling
- `RpcClientTests.cs` - Client calls, batch
- `MiddlewareTests.cs` - Pipeline, order
- `ProtocolCompatibilityTests.cs` - Envelope validation, notifications, mixed batches, null fields, and Safe Mode boundaries
- Schema validation tests - Planned expansion; there is no standalone `ValidationTests.cs` file yet

### Integration Tests

- Cross-platform (Node.js, PHP, .NET)
- Performance benchmarks
- Memory leak detection

## 🚀 Historical Bootstrap Roadmap

The original checklist below is preserved as planning history, not current implementation status. Core endpoint/client, middleware, batch handling, authentication, rate limiting, ASP.NET Core integration, examples, and CI are now implemented. See [PROJECT_PLAN.md](PROJECT_PLAN.md) for current gaps and [CHANGELOG.md](CHANGELOG.md) for release history.

### Phase 1: Core (Original Status: In Progress)
- [x] Project structure
- [x] Multi-targeting
- [x] Serialization factory
- [x] Safe mode implementation
- [x] Exception hierarchy
- [ ] RpcEndpoint class
- [ ] RpcClient class
- [ ] Basic tests

### Phase 2: Features
- [ ] Middleware system
- [ ] Schema validation
- [ ] Batch processing
- [ ] Logging integration

### Phase 3: Enterprise
- [ ] Rate limiting
- [ ] CORS
- [ ] Authentication
- [ ] ASP.NET Core integration

### Phase 4: Release
- [ ] Complete documentation
- [ ] Examples
- [ ] CI/CD
- [ ] NuGet publish

## 📝 Breaking Changes Policy

**v1.x**: Retain multi-targeting for broad compatibility.

**v2.0**: Dropping .NET Standard 2.0 and focusing on modern .NET is a possibility, not a committed change.

When introducing a breaking change:

1. Add a deprecation warning in v1.x
2. Document the change in CHANGELOG
3. Provide a migration guide
4. Major version bump

## 🔗 Cross-Platform Compatibility

The toolkit targets the shared JSON-RPC contract with:

- **rpc-express-toolkit** (Node.js)
- **rpc-php-toolkit** (PHP)

Compatibility must be checked against the actual versions under test; this is not a blanket claim that every runtime/version combination has been validated.

Compatibility test directions:

- .NET → Node.js RPC calls
- .NET → PHP RPC calls
- Node.js → .NET RPC calls
- PHP → .NET RPC calls

## 📚 Resources

- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)
- [.NET Multi-Targeting](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)
- [System.Text.Json Performance](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/performance)
