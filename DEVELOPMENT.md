# RPC .NET Toolkit - Development Notes

## 🏗️ Architecture

### Multi-Targeting Strategy

Il progetto supporta 3 framework target:
- **.NET Standard 2.0** - Compatibilità massima (Unity, Xamarin, .NET Framework 4.6.1+)
- **.NET 6.0** - LTS moderno
- **.NET 8.0** - Ultima versione stabile

### Conditional Compilation

Usiamo `#if` per compilare codice diverso per framework:

```csharp
#if NETSTANDARD2_0
    // Codice per .NET Standard 2.0
    using Newtonsoft.Json;
#else
    // Codice per .NET 6+ (.NET 6.0 e 8.0)
    using System.Text.Json;
#endif
```

Oppure con `NET6_0_OR_GREATER`:

```csharp
#if NET6_0_OR_GREATER
    // Solo .NET 6+
    public async ValueTask<object> HandleAsync(...)
#else
    // .NET Standard 2.0
    public async Task<object> HandleAsync(...)
#endif
```

### Serialization

| Framework | Serializer | Performance | Note |
|-----------|------------|-------------|------|
| netstandard2.0 | Newtonsoft.Json | ⭐⭐⭐ | Compatibility-first |
| net6.0 | System.Text.Json | ⭐⭐⭐⭐ | Modern + Fast |
| net8.0 | System.Text.Json | ⭐⭐⭐⭐⭐ | Latest perf |

Il `SerializerFactory` sceglie automaticamente il serializer giusto a compile-time.

### Safe Mode

La serializzazione Safe Mode aggiunge prefissi:
- **Strings**: `"hello"` → `"S:hello"`
- **Dates**: `DateTime` → `"D:2025-11-26T10:30:00Z"`
- **BigInteger**: `9007199254740992` → `"9007199254740992n"`

Questo previene la confusion tra stringhe e valori numerici/date quando si fanno chiamate RPC cross-platform.

## 🔧 Development Setup

### Requirements

- .NET 8.0 SDK (per sviluppo e testing)
- Visual Studio 2022 / VS Code / Rider

### Build

```bash
# Restore dependencies
dotnet restore

# Build all frameworks
dotnet build

# Build specifico framework
dotnet build -f netstandard2.0
dotnet build -f net6.0
dotnet build -f net8.0
```

### Test

```bash
# Run tests
dotnet test

# Run con coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Package

```bash
# Create NuGet package
dotnet pack -c Release -o ./nupkg

# Publish to local feed
dotnet nuget push ./nupkg/RpcToolkit.1.0.0.nupkg -s ~/nuget-local

# Publish to NuGet.org
dotnet nuget push ./nupkg/RpcToolkit.1.0.0.nupkg -s https://api.nuget.org/v3/index.json -k YOUR_API_KEY
```

## 📊 Performance Targets

### Benchmarks Goal (.NET 8 vs .NET Standard 2.0)

| Operation | Target Improvement |
|-----------|-------------------|
| Simple RPC call | 2.5x faster |
| Batch 100 items | 2.8x faster |
| Serialization | 3.0x faster |
| Memory allocation | 40% less |

### Optimization Techniques (.NET 6+)

1. **Span<T>** per zero-allocation parsing
2. **ArrayPool** per buffer reuse
3. **ValueTask<T>** per hot paths
4. **System.Text.Json** built-in
5. **ReadOnlySpan** per string operations

## 🧪 Testing Strategy

### Unit Tests

- `SerializationTests.cs` - Safe mode, round-trip
- `RpcEndpointTests.cs` - Method registration, handling
- `RpcClientTests.cs` - Client calls, batch
- `MiddlewareTests.cs` - Pipeline, order
- `ValidationTests.cs` - Schema validation

### Integration Tests

- Cross-platform (Node.js, PHP, .NET)
- Performance benchmarks
- Memory leak detection

## 🚀 Roadmap

### Phase 1: Core (In Progress)
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
- [ ] Documentation completa
- [ ] Examples
- [ ] CI/CD
- [ ] NuGet publish

## 📝 Breaking Changes Policy

**v1.x**: Multi-targeting mantenuto per compatibilità massima
**v2.0**: Possibile drop di .NET Standard 2.0, focus su .NET 6+ only

Quando si introduce un breaking change:
1. Aggiungere deprecation warning in v1.x
2. Documentare in CHANGELOG
3. Fornire migration guide
4. Major version bump

## 🔗 Cross-Platform Compatibility

Il toolkit deve essere 100% compatibile con:
- **rpc-express-toolkit** (Node.js) v4.2.0
- **rpc-php-toolkit** (PHP) v1.1.0

Test di compatibilità:
- .NET → Node.js RPC calls
- .NET → PHP RPC calls
- Node.js → .NET RPC calls
- PHP → .NET RPC calls

## 📚 Resources

- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)
- [.NET Multi-Targeting](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)
- [System.Text.Json Performance](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/performance)
