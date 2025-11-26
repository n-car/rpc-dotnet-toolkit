# Enhanced Features Examples

This document demonstrates the new enhanced features added to rpc-dotnet-toolkit to achieve feature parity with rpc-express-toolkit.

## Enhanced Logging System

The new `RpcLogger` provides structured logging with multiple levels, formats, and console color support.

### Basic Usage

```csharp
using RpcToolkit;
using RpcToolkit.Logging;

// Configure logging options
var options = new RpcOptions
{
    LoggerOptions = new RpcLoggerOptions
    {
        Level = RpcLogLevel.Info,           // Minimum log level
        Format = RpcLogFormat.Text,         // Text or JSON output
        IncludeTimestamp = true,            // Add timestamps
        IncludeRequestId = true,            // Add request IDs
        Prefix = "[MyApp]"                  // Custom prefix
    }
};

var endpoint = new RpcEndpoint(options);
```

### Log Levels

```csharp
// Available log levels (in order of verbosity)
RpcLogLevel.Silent   // No logging
RpcLogLevel.Error    // Only errors
RpcLogLevel.Warn     // Warnings and errors
RpcLogLevel.Info     // Info, warnings, and errors (default)
RpcLogLevel.Debug    // Debug messages plus all above
RpcLogLevel.Trace    // All messages including trace
```

### JSON Format Output

```csharp
var options = new RpcOptions
{
    LoggerOptions = new RpcLoggerOptions
    {
        Format = RpcLogFormat.Json  // Output logs as JSON
    }
};
```

Output example:
```json
{
  "level": "info",
  "message": "RPC Endpoint initialized",
  "timestamp": "2024-01-15T10:30:00Z",
  "data": {
    "version": "2.0"
  }
}
```

## Advanced Batch Options

Enhanced batch processing with parallel execution control, error handling, and metrics collection.

### Basic Batch Configuration

```csharp
var options = new RpcOptions
{
    BatchOptions = new RpcBatchOptions
    {
        MaxSize = 50,                       // Maximum batch size
        Parallel = true,                    // Execute in parallel
        MaxParallelism = 10,                // Max concurrent tasks
        ContinueOnError = true,             // Don't stop on first error
        TimeoutSeconds = 30,                // Batch timeout
        CollectMetrics = true               // Collect performance metrics
    }
};

var endpoint = new RpcEndpoint(options);
```

### Sequential Processing

```csharp
var options = new RpcOptions
{
    BatchOptions = new RpcBatchOptions
    {
        Parallel = false,           // Process requests sequentially
        ContinueOnError = false     // Stop on first error
    }
};
```

### Batch Metrics

When `CollectMetrics` is enabled, the system collects detailed statistics:

```csharp
// Metrics collected automatically:
// - TotalRequests: Number of requests in batch
// - SuccessCount: Successful requests
// - ErrorCount: Failed requests
// - TotalDurationMs: Total batch duration
// - AverageDurationMs: Average request duration
// - MinDurationMs: Fastest request
// - MaxDurationMs: Slowest request
// - RequestTimings: Per-request timing details
```

## Enhanced Middleware

### Timing Middleware

Measures and logs execution time of RPC requests.

```csharp
using RpcToolkit.Middleware;

// Basic usage
endpoint.UseTiming();

// With custom options
endpoint.UseTiming(new TimingMiddlewareOptions
{
    MinDurationMs = 100,                     // Only log requests > 100ms
    SlowRequestThresholdMs = 1000,           // Warn for requests > 1 second
    SlowRequestLogLevel = RpcLogLevel.Warn,  // Log level for slow requests
    IncludeMethod = true,                    // Include method name
    IncludeParams = false,                   // Don't log parameters (security)
    LogLevel = RpcLogLevel.Info              // Default log level
});

// Inline configuration
endpoint.UseTiming(options =>
{
    options.MinDurationMs = 50;
    options.SlowRequestThresholdMs = 500;
});
```

Output example:
```
[2024-01-15 10:30:00] info: RPC request user.getById completed in 145ms
[2024-01-15 10:30:01] warn: Slow RPC request products.search completed in 1250ms
```

### Method Whitelist Middleware

Restricts which RPC methods can be called, supporting wildcard patterns.

```csharp
// Allow specific methods only
endpoint.UseMethodWhitelist("user.getById", "user.list", "products.*");

// With custom options
endpoint.UseMethodWhitelist(options =>
{
    options.AllowedMethods = new List<string>
    {
        "user.*",           // All user methods
        "*.list",           // All list methods
        "products.getById"  // Specific method
    };
    options.CaseSensitive = false;
    options.ErrorMessage = "Access denied to method: {method}";
});

// Blacklist specific methods
endpoint.UseMethodBlacklist("admin.*", "*.delete", "system.shutdown");

// Combined whitelist and blacklist
endpoint.UseMethodWhitelist(options =>
{
    options.AllowedMethods = new List<string> { "user.*", "products.*" };
    options.DeniedMethods = new List<string> { "*.delete", "*.update" };
});
```

#### Wildcard Patterns

- `"user.*"` - All methods starting with "user."
- `"*.list"` - All methods ending with ".list"
- `"user.*.delete"` - Methods starting with "user." and ending with ".delete"
- `"exact.method.name"` - Exact match only

## Complete Example

```csharp
using RpcToolkit;
using RpcToolkit.Logging;
using RpcToolkit.Middleware;

var endpoint = new RpcEndpoint(new RpcOptions
{
    // Enhanced logging
    LoggerOptions = new RpcLoggerOptions
    {
        Level = RpcLogLevel.Info,
        Format = RpcLogFormat.Json,
        IncludeTimestamp = true,
        IncludeRequestId = true
    },
    
    // Advanced batch processing
    BatchOptions = new RpcBatchOptions
    {
        MaxSize = 100,
        Parallel = true,
        MaxParallelism = 10,
        ContinueOnError = true,
        CollectMetrics = true
    }
});

// Add timing middleware
endpoint.UseTiming(options =>
{
    options.MinDurationMs = 10;
    options.SlowRequestThresholdMs = 500;
});

// Restrict methods
endpoint.UseMethodWhitelist("user.*", "products.*", "orders.getById");

// Register methods
endpoint.AddMethod("user.getById", async (int id) =>
{
    return new { Id = id, Name = "John Doe" };
});

endpoint.AddMethod("user.list", async () =>
{
    return new[] { 
        new { Id = 1, Name = "John" }, 
        new { Id = 2, Name = "Jane" } 
    };
});

endpoint.AddMethod("products.search", async (string query) =>
{
    await Task.Delay(100); // Simulate database query
    return new[] { 
        new { Id = 1, Name = query, Price = 99.99 } 
    };
});

// Process requests
var request = @"{
    ""jsonrpc"": ""2.0"",
    ""method"": ""user.getById"",
    ""params"": { ""id"": 1 },
    ""id"": 1
}";

var response = await endpoint.HandleRequestAsync(request);
Console.WriteLine(response);
```

## Migration from Basic Configuration

### Before (Basic)
```csharp
var endpoint = new RpcEndpoint(new RpcOptions
{
    MaxBatchSize = 50
});
```

### After (Enhanced)
```csharp
var endpoint = new RpcEndpoint(new RpcOptions
{
    // Old property still works for backwards compatibility
    MaxBatchSize = 50,
    
    // Or use new advanced options
    BatchOptions = new RpcBatchOptions
    {
        MaxSize = 50,
        Parallel = true,
        MaxParallelism = 10,
        ContinueOnError = true,
        CollectMetrics = true
    },
    
    // Add logging
    LoggerOptions = new RpcLoggerOptions
    {
        Level = RpcLogLevel.Info,
        Format = RpcLogFormat.Text
    }
});

// Add middleware
endpoint.UseTiming();
endpoint.UseMethodWhitelist("*"); // Allow all methods
```

## Performance Considerations

### Parallel Batch Processing

- Use `Parallel = true` with `MaxParallelism` to control CPU usage
- Set `MaxParallelism = Environment.ProcessorCount` for CPU-bound operations
- Set higher `MaxParallelism` for I/O-bound operations

### Logging Performance

- Use `RpcLogLevel.Info` or higher in production
- Use `RpcLogFormat.Text` for better console performance
- Use `RpcLogFormat.Json` for structured log aggregation systems

### Middleware Order

Middleware executes in registration order. Recommended order:
1. Authentication middleware
2. Method whitelist middleware
3. Timing middleware
4. Schema validation middleware
5. Rate limiting middleware

```csharp
endpoint
    .UseAuth(authenticator)
    .UseMethodWhitelist("user.*", "products.*")
    .UseTiming()
    .UseSchemaValidation(schemas)
    .UseRateLimit(maxRequests: 100, windowMs: 60000);
```
