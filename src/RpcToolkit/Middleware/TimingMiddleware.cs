using System;
using System.Diagnostics;
using System.Threading.Tasks;
using RpcToolkit.Logging;

namespace RpcToolkit.Middleware
{
    /// <summary>
    /// Options for configuring the timing middleware.
    /// </summary>
    public class TimingMiddlewareOptions
    {
        /// <summary>
        /// Minimum duration in milliseconds to log. Requests faster than this will not be logged.
        /// Default is 0 (log all requests).
        /// </summary>
        public long MinDurationMs { get; set; } = 0;

        /// <summary>
        /// Log level to use for timing information.
        /// Default is Info.
        /// </summary>
        public RpcLogLevel LogLevel { get; set; } = RpcLogLevel.Info;

        /// <summary>
        /// Include the method name in the timing log.
        /// Default is true.
        /// </summary>
        public bool IncludeMethod { get; set; } = true;

        /// <summary>
        /// Include request parameters in the timing log.
        /// Default is false (for security/privacy).
        /// </summary>
        public bool IncludeParams { get; set; } = false;

        /// <summary>
        /// Log slow requests at a higher log level.
        /// Default is 1000ms (1 second).
        /// </summary>
        public long SlowRequestThresholdMs { get; set; } = 1000;

        /// <summary>
        /// Log level for slow requests.
        /// Default is Warn.
        /// </summary>
        public RpcLogLevel SlowRequestLogLevel { get; set; } = RpcLogLevel.Warn;
    }

    /// <summary>
    /// Middleware that measures and logs the execution time of RPC requests.
    /// Similar to Express.js timing middleware.
    /// </summary>
    public class TimingMiddleware : IMiddleware
    {
        private readonly TimingMiddlewareOptions _options;
        private readonly RpcLogger? _logger;
        private readonly Stopwatch _stopwatch;
        private DateTime _startTime;

        /// <summary>
        /// Creates a new timing middleware with default options.
        /// </summary>
        public TimingMiddleware() : this(new TimingMiddlewareOptions())
        {
        }

        /// <summary>
        /// Creates a new timing middleware with specified options.
        /// </summary>
        /// <param name="options">Configuration options for the timing middleware.</param>
        /// <param name="logger">Optional logger for output. If null, creates a new logger.</param>
        public TimingMiddleware(TimingMiddlewareOptions options, RpcLogger? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? new RpcLogger(new RpcLoggerOptions
            {
                Level = options.LogLevel,
                IncludeTimestamp = true
            }, null);
            _stopwatch = new Stopwatch();
        }

        /// <summary>
        /// Execute before the RPC method is called.
        /// </summary>
        public Task BeforeAsync(RpcRequest request, object? context)
        {
            _startTime = DateTime.UtcNow;
            _stopwatch.Restart();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Execute after the RPC method is called.
        /// </summary>
        public Task AfterAsync(RpcRequest request, object? result, object? context)
        {
            _stopwatch.Stop();
            LogTiming(request, _stopwatch.ElapsedMilliseconds, _startTime, null);
            return Task.CompletedTask;
        }

        private void LogTiming(RpcRequest request, long durationMs, DateTime startTime, Exception? exception)
        {
            // Skip if below minimum duration threshold
            if (durationMs < _options.MinDurationMs)
            {
                return;
            }

            // Determine log level based on duration and error
            var logLevel = _options.LogLevel;
            if (exception != null)
            {
                logLevel = RpcLogLevel.Error;
            }
            else if (durationMs >= _options.SlowRequestThresholdMs)
            {
                logLevel = _options.SlowRequestLogLevel;
            }

            // Build log data
            var logData = new
            {
                DurationMs = durationMs,
                StartTime = startTime,
                Method = _options.IncludeMethod ? request.Method : null,
                RequestId = request.Id,
                Params = _options.IncludeParams ? request.Params : null,
                IsSlow = durationMs >= _options.SlowRequestThresholdMs
            };

            var message = exception != null
                ? $"RPC request {request.Method} failed after {durationMs}ms"
                : durationMs >= _options.SlowRequestThresholdMs
                    ? $"Slow RPC request {request.Method} completed in {durationMs}ms"
                    : $"RPC request {request.Method} completed in {durationMs}ms";

            _logger?.Log(logLevel, message, logData, exception);
        }
    }

    /// <summary>
    /// Extension method for easy timing middleware registration.
    /// </summary>
    public static class TimingMiddlewareExtensions
    {
        /// <summary>
        /// Adds timing middleware to the RPC endpoint.
        /// </summary>
        /// <param name="endpoint">The RPC endpoint.</param>
        /// <param name="options">Optional configuration options.</param>
        /// <returns>The endpoint for chaining.</returns>
        public static RpcEndpoint UseTiming(this RpcEndpoint endpoint, TimingMiddlewareOptions? options = null)
        {
            endpoint.GetMiddleware()?.Add(new TimingMiddleware(options ?? new TimingMiddlewareOptions()));
            return endpoint;
        }

        /// <summary>
        /// Adds timing middleware with inline configuration.
        /// </summary>
        /// <param name="endpoint">The RPC endpoint.</param>
        /// <param name="configure">Configuration action.</param>
        /// <returns>The endpoint for chaining.</returns>
        public static RpcEndpoint UseTiming(this RpcEndpoint endpoint, Action<TimingMiddlewareOptions> configure)
        {
            var options = new TimingMiddlewareOptions();
            configure(options);
            endpoint.GetMiddleware()?.Add(new TimingMiddleware(options));
            return endpoint;
        }
    }
}
