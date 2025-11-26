using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpcToolkit.Exceptions;
using RpcToolkit.Middleware;
using RpcToolkit.Serialization;
using RpcToolkit.Logging;
using System.Diagnostics;

namespace RpcToolkit
{
    /// <summary>
    /// JSON-RPC 2.0 server endpoint
    /// </summary>
    public class RpcEndpoint
    {
        private readonly Dictionary<string, MethodHandler> _methods = new();
        private readonly RpcOptions _options;
        private readonly ILogger? _logger;
        private readonly RpcLogger? _rpcLogger;
        private readonly MiddlewareManager? _middleware;
        private readonly Validation.SchemaValidator? _validator;
        private readonly object? _context;
        private readonly RpcBatchOptions _batchOptions;
        private readonly string _introspectionPrefix;
        private bool _isInternalRegistration = false;

        /// <summary>
        /// Create a new RPC endpoint
        /// </summary>
        /// <param name="context">Context object passed to method handlers</param>
        /// <param name="options">Configuration options</param>
        /// <param name="logger">Optional logger</param>
        public RpcEndpoint(object? context = null, RpcOptions? options = null, ILogger? logger = null)
        {
            _context = context;
            _options = options ?? new RpcOptions();
            _logger = logger;
            _batchOptions = _options.BatchOptions;
            _introspectionPrefix = _options.IntrospectionPrefix;

            if (_options.EnableLogging)
            {
                _rpcLogger = new RpcLogger(_options.LoggerOptions, _logger);
                _rpcLogger.Info("RpcEndpoint initialized");
            }

            if (_options.EnableMiddleware)
            {
                _middleware = new MiddlewareManager();
            }

            if (_options.EnableValidation)
            {
                _validator = new Validation.SchemaValidator();
            }

            if (_options.EnableIntrospection)
            {
                RegisterIntrospectionMethods();
            }
        }

        /// <summary>
        /// Get the middleware manager
        /// </summary>
        public MiddlewareManager? GetMiddleware() => _middleware;

        /// <summary>
        /// Register an RPC method with optional schema and metadata
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="handler">Method handler function</param>
        /// <param name="config">Optional method configuration (schema, exposeSchema, description)</param>
        public RpcEndpoint AddMethod<TParams, TResult>(
            string methodName,
            Func<TParams?, object?, Task<TResult>> handler,
            MethodConfig? config = null)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("Method name cannot be empty", nameof(methodName));

            // Prevent users from registering introspection methods
            if (methodName.StartsWith(_introspectionPrefix + ".") && !_isInternalRegistration)
                throw new InvalidOperationException($"Method names starting with '{_introspectionPrefix}.' are reserved for RPC introspection");

            if (_methods.ContainsKey(methodName))
                throw new InvalidOperationException($"Method '{methodName}' is already registered");

            _methods[methodName] = new MethodHandler
            {
                Name = methodName,
                Handler = async (paramsObj, ctx) =>
                {
                    TParams? typedParams = default;
                    if (paramsObj != null)
                    {
                        var json = SerializerFactory.Serialize(paramsObj, _options.SafeEnabled);
                        typedParams = SerializerFactory.Deserialize<TParams>(json, _options.SafeEnabled);
                    }
                    return await handler(typedParams, ctx);
                },
                Schema = config?.Schema,
                ExposeSchema = config?.ExposeSchema ?? false,
                Description = config?.Description
            };

            _logger?.LogDebug("Method registered: {MethodName}", methodName);
            return this;
        }

        /// <summary>
        /// Register a synchronous RPC method with optional schema and metadata
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="handler">Method handler function</param>
        /// <param name="config">Optional method configuration (schema, exposeSchema, description)</param>
        public RpcEndpoint AddMethod<TParams, TResult>(
            string methodName,
            Func<TParams?, object?, TResult> handler,
            MethodConfig? config = null)
        {
            return AddMethod<TParams, TResult>(methodName, (p, ctx) => Task.FromResult(handler(p, ctx)), config);
        }

        /// <summary>
        /// Remove a registered method
        /// </summary>
        public RpcEndpoint RemoveMethod(string methodName)
        {
            _methods.Remove(methodName);
            _logger?.LogDebug("Method removed: {MethodName}", methodName);
            return this;
        }

        /// <summary>
        /// Get all registered method names
        /// </summary>
        public IEnumerable<string> GetMethods() => _methods.Keys;

        /// <summary>
        /// List all registered method names
        /// </summary>
        public string[] ListMethods() => _methods.Keys.ToArray();

        /// <summary>
        /// Get method configuration by name
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <returns>Method configuration or null if not found</returns>
        public MethodConfig? GetMethod(string methodName)
        {
            if (!_methods.TryGetValue(methodName, out var handler))
                return null;

            return new MethodConfig
            {
                Name = handler.Name,
                Handler = handler.Handler,
                Schema = handler.Schema,
                ExposeSchema = handler.ExposeSchema,
                Description = handler.Description
            };
        }

        /// <summary>
        /// Get the logger instance
        /// </summary>
        public RpcLogger? GetLogger() => _rpcLogger;

        /// <summary>
        /// Get the schema validator instance
        /// </summary>
        public Validation.SchemaValidator? GetValidator() => _validator;

        /// <summary>
        /// Handle a JSON-RPC request
        /// </summary>
        public async Task<string> HandleRequestAsync(string jsonRequest)
        {
            try
            {
                // Parse request
                var isBatch = jsonRequest.TrimStart().StartsWith("[");

                if (isBatch)
                {
                    if (!_options.EnableBatch)
                    {
                        var error = new InvalidRequestException("Batch requests are not enabled");
                        return SerializerFactory.Serialize(CreateErrorResponse(null, error), _options.SafeEnabled);
                    }

                    var requests = SerializerFactory.Deserialize<List<RpcRequest>>(jsonRequest, _options.SafeEnabled);
                    if (requests == null || requests.Count == 0)
                    {
                        var error = new InvalidRequestException("Invalid batch request");
                        return SerializerFactory.Serialize(CreateErrorResponse(null, error), _options.SafeEnabled);
                    }

                    if (requests.Count > _batchOptions.MaxSize && _batchOptions.MaxSize > 0)
                    {
                        var error = new InvalidRequestException($"Batch size exceeds maximum of {_batchOptions.MaxSize}");
                        return SerializerFactory.Serialize(CreateErrorResponse(null, error), _options.SafeEnabled);
                    }

                    var responses = await HandleBatchAsync(requests);
                    return SerializerFactory.Serialize(responses, _options.SafeEnabled);
                }
                else
                {
                    var request = SerializerFactory.Deserialize<RpcRequest>(jsonRequest, _options.SafeEnabled);
                    if (request == null)
                    {
                        var error = new InvalidRequestException("Invalid request format");
                        return SerializerFactory.Serialize(CreateErrorResponse(null, error), _options.SafeEnabled);
                    }

                    var response = await HandleSingleRequestAsync(request);
                    
                    // Notifications return no response
                    if (request.IsNotification)
                        return string.Empty;

                    return SerializerFactory.Serialize(response, _options.SafeEnabled);
                }
            }
            catch (Exception ex) when (ex is not RpcException)
            {
                _logger?.LogError(ex, "Error parsing request");
                var error = new ParseErrorException(ex);
                return SerializerFactory.Serialize(CreateErrorResponse(null, error), _options.SafeEnabled);
            }
        }

        private async Task<RpcResponse> HandleSingleRequestAsync(RpcRequest request)
        {
            try
            {
                // Validate request
                ValidateRequest(request);

                // Execute middleware before
                if (_middleware != null)
                {
                    await _middleware.ExecuteBeforeAsync(request, _context);
                }

                // Find method
                if (!_methods.TryGetValue(request.Method, out var methodHandler))
                {
                    throw new MethodNotFoundException(request.Method);
                }

                // Execute method
                var result = await methodHandler.Handler(request.Params, _context);

                // Execute middleware after
                if (_middleware != null)
                {
                    await _middleware.ExecuteAfterAsync(request, result, _context);
                }

                _logger?.LogDebug("Method executed successfully: {Method}", request.Method);

                return new RpcResponse
                {
                    JsonRpc = "2.0",
                    Result = result,
                    Id = request.Id
                };
            }
            catch (RpcException ex)
            {
                _logger?.LogWarning(ex, "RPC error: {Message}", ex.Message);
                return CreateErrorResponse(request.Id, ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Internal error processing request");
                var error = _options.SanitizeErrors
                    ? new InternalErrorException("Internal error")
                    : new InternalErrorException(ex.Message, ex);
                return CreateErrorResponse(request.Id, error);
            }
        }

        private async Task<List<RpcResponse>> HandleBatchAsync(List<RpcRequest> requests)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var metrics = _batchOptions.CollectMetrics ? new RpcBatchMetrics
            {
                TotalRequests = requests.Count,
                StartTime = startTime
            } : null;

            _rpcLogger!.Info($"Processing batch request with {requests.Count} items", new
            {
                BatchSize = requests.Count,
                Parallel = _batchOptions.Parallel,
                MaxParallelism = _batchOptions.MaxParallelism
            });

            var responses = new List<RpcResponse>();

            try
            {
                if (_batchOptions.Parallel)
                {
                    // Parallel execution with optional max parallelism
                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = _batchOptions.MaxParallelism > 0 
                            ? _batchOptions.MaxParallelism 
                            : Environment.ProcessorCount
                    };

                    var tasks = requests.Select(async request =>
                    {
                        var requestStopwatch = Stopwatch.StartNew();
                        try
                        {
                            var response = await HandleSingleRequestAsync(request);
                            
                            if (metrics != null)
                            {
                                requestStopwatch.Stop();
                                lock (metrics.RequestTimings)
                                {
                                    metrics.SuccessCount++;
                                    metrics.RequestTimings.Add(new RequestTiming
                                    {
                                        Id = request.Id,
                                        Method = request.Method,
                                        DurationMs = requestStopwatch.ElapsedMilliseconds,
                                        Success = response.Error == null
                                    });
                                }
                            }
                            
                            return response;
                        }
                        catch (Exception ex)
                        {
                            requestStopwatch.Stop();
                            
                            if (metrics != null)
                            {
                                lock (metrics.RequestTimings)
                                {
                                    metrics.ErrorCount++;
                                    metrics.RequestTimings.Add(new RequestTiming
                                    {
                                        Id = request.Id,
                                        Method = request.Method,
                                        DurationMs = requestStopwatch.ElapsedMilliseconds,
                                        Success = false,
                                        ErrorMessage = ex.Message
                                    });
                                }
                            }

                            if (!_batchOptions.ContinueOnError)
                            {
                                throw;
                            }

                            _rpcLogger.Error($"Error in batch request: {request.Method}", new { RequestId = request.Id }, ex);
                            return CreateErrorResponse(request.Id, new InternalErrorException());
                        }
                    });

                    responses = (await Task.WhenAll(tasks)).ToList();
                }
                else
                {
                    // Sequential execution
                    foreach (var request in requests)
                    {
                        var requestStopwatch = Stopwatch.StartNew();
                        try
                        {
                            var response = await HandleSingleRequestAsync(request);
                            responses.Add(response);
                            
                            if (metrics != null)
                            {
                                requestStopwatch.Stop();
                                metrics.SuccessCount++;
                                metrics.RequestTimings.Add(new RequestTiming
                                {
                                    Id = request.Id,
                                    Method = request.Method,
                                    DurationMs = requestStopwatch.ElapsedMilliseconds,
                                    Success = response.Error == null
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            requestStopwatch.Stop();
                            
                            if (metrics != null)
                            {
                                metrics.ErrorCount++;
                                metrics.RequestTimings.Add(new RequestTiming
                                {
                                    Id = request.Id,
                                    Method = request.Method,
                                    DurationMs = requestStopwatch.ElapsedMilliseconds,
                                    Success = false,
                                    ErrorMessage = ex.Message
                                });
                            }

                            if (!_batchOptions.ContinueOnError)
                            {
                                throw;
                            }

                            _rpcLogger.Error($"Error in batch request: {request.Method}", new { RequestId = request.Id }, ex);
                            responses.Add(CreateErrorResponse(request.Id, new InternalErrorException()));
                        }
                    }
                }

                // Calculate final metrics
                if (metrics != null)
                {
                    stopwatch.Stop();
                    metrics.EndTime = DateTime.UtcNow;
                    metrics.TotalDurationMs = stopwatch.ElapsedMilliseconds;
                    
                    if (metrics.RequestTimings.Count > 0)
                    {
                        metrics.AverageDurationMs = metrics.RequestTimings.Average(t => t.DurationMs);
                        metrics.MinDurationMs = metrics.RequestTimings.Min(t => t.DurationMs);
                        metrics.MaxDurationMs = metrics.RequestTimings.Max(t => t.DurationMs);
                    }

                    _rpcLogger.Info("Batch completed", new
                    {
                        TotalRequests = metrics.TotalRequests,
                        SuccessCount = metrics.SuccessCount,
                        ErrorCount = metrics.ErrorCount,
                        TotalDurationMs = metrics.TotalDurationMs,
                        AverageDurationMs = metrics.AverageDurationMs
                    });
                }
            }
            catch (Exception ex)
            {
                _rpcLogger.Error("Batch processing failed", null, ex);
                throw;
            }

            return responses;
        }

        private void ValidateRequest(RpcRequest request)
        {
            if (request.JsonRpc != "2.0")
            {
                throw new InvalidRequestException("Invalid JSON-RPC version. Must be '2.0'");
            }

            if (string.IsNullOrWhiteSpace(request.Method))
            {
                throw new InvalidRequestException("Method name is required");
            }
        }

        private RpcResponse CreateErrorResponse(object? id, RpcException exception)
        {
            return new RpcResponse
            {
                JsonRpc = "2.0",
                Error = exception.ToRpcError(),
                Id = id
            };
        }

        /// <summary>
        /// Register introspection methods (__rpc.*)
        /// </summary>
        private void RegisterIntrospectionMethods()
        {
            _isInternalRegistration = true;

            // __rpc.listMethods - List all user methods (excludes __rpc.* methods)
            AddMethod<object, string[]>(
                $"{_introspectionPrefix}.listMethods",
                (_, __) =>
                {
                    return _methods.Keys
                        .Where(name => !name.StartsWith(_introspectionPrefix + "."))
                        .ToArray();
                },
                new MethodConfig
                {
                    Description = "List all available RPC methods",
                    ExposeSchema = true
                }
            );

            // __rpc.describe - Get schema and description of a specific method
            AddMethod<DescribeParams, object>(
                $"{_introspectionPrefix}.describe",
                (p, _) =>
                {
                    if (p == null || string.IsNullOrEmpty(p.Method))
                        throw new InvalidParamsException("Method name required");

                    if (!_methods.TryGetValue(p.Method, out var handler))
                        throw new MethodNotFoundException(p.Method);

                    return new
                    {
                        name = handler.Name,
                        schema = handler.Schema,
                        description = handler.Description ?? ""
                    };
                },
                new MethodConfig
                {
                    Description = "Get schema and description of a specific method",
                    ExposeSchema = true,
                    Schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            method = new { type = "string" }
                        },
                        required = new[] { "method" }
                    }
                }
            );

            // __rpc.describeAll - Get all methods with public schemas
            AddMethod<object, object[]>(
                $"{_introspectionPrefix}.describeAll",
                (_, __) =>
                {
                    return _methods.Values
                        .Where(m => !m.Name.StartsWith(_introspectionPrefix + ".") && m.ExposeSchema)
                        .Select(m => new
                        {
                            name = m.Name,
                            schema = m.Schema,
                            description = m.Description ?? ""
                        })
                        .ToArray<object>();
                },
                new MethodConfig
                {
                    Description = "List all methods with public schemas",
                    ExposeSchema = true
                }
            );

            // __rpc.version - Get toolkit version
            AddMethod<object, object>(
                $"{_introspectionPrefix}.version",
                (_, __) =>
                {
                    var assembly = typeof(RpcEndpoint).Assembly;
                    var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
                    var frameworkVersion = Environment.Version.ToString();

                    return new
                    {
                        toolkit = "rpc-dotnet-toolkit",
                        version = version,
                        dotnetVersion = frameworkVersion
                    };
                },
                new MethodConfig
                {
                    Description = "Get RPC toolkit version information",
                    ExposeSchema = true
                }
            );

            // __rpc.capabilities - Get server capabilities
            AddMethod<object, object>(
                $"{_introspectionPrefix}.capabilities",
                (_, __) =>
                {
                    var methodCount = _methods.Keys.Count(name => !name.StartsWith(_introspectionPrefix + "."));
                    
                    return new
                    {
                        batch = _options.EnableBatch,
                        introspection = true,
                        validation = _options.EnableValidation,
                        middleware = _options.EnableMiddleware,
                        safeMode = _options.SafeEnabled,
                        methodCount = methodCount
                    };
                },
                new MethodConfig
                {
                    Description = "Get server capabilities and configuration",
                    ExposeSchema = true
                }
            );

            _isInternalRegistration = false;
        }

        private class DescribeParams
        {
            public string Method { get; set; } = string.Empty;
        }
    }
}
