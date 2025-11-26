using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RpcToolkit.Logging
{
    /// <summary>
    /// Log levels matching Express-style logging
    /// </summary>
    public enum RpcLogLevel
    {
        /// <summary>No logging</summary>
        Silent = 0,
        /// <summary>Only errors</summary>
        Error = 1,
        /// <summary>Warnings and errors</summary>
        Warn = 2,
        /// <summary>Informational messages</summary>
        Info = 3,
        /// <summary>Debugging information</summary>
        Debug = 4,
        /// <summary>Trace-level logging</summary>
        Trace = 5
    }

    /// <summary>
    /// Log output format
    /// </summary>
    public enum RpcLogFormat
    {
        /// <summary>Human-readable text format</summary>
        Text,
        /// <summary>Structured JSON format</summary>
        Json
    }

    /// <summary>
    /// Logging configuration options
    /// </summary>
    public class RpcLoggerOptions
    {
        /// <summary>Minimum log level</summary>
        public RpcLogLevel Level { get; set; } = RpcLogLevel.Info;

        /// <summary>Output format</summary>
        public RpcLogFormat Format { get; set; } = RpcLogFormat.Text;

        /// <summary>Include timestamps</summary>
        public bool IncludeTimestamp { get; set; } = true;

        /// <summary>Include request ID</summary>
        public bool IncludeRequestId { get; set; } = true;

        /// <summary>Include method name</summary>
        public bool IncludeMethod { get; set; } = true;

        /// <summary>Include duration for timed operations</summary>
        public bool IncludeDuration { get; set; } = true;

        /// <summary>Custom prefix for log messages</summary>
        public string? Prefix { get; set; }
    }

    /// <summary>
    /// Structured logger for RPC operations
    /// </summary>
    public class RpcLogger
    {
        private readonly RpcLoggerOptions _options;
        private readonly ILogger? _logger;

        /// <summary>
        /// Creates a new RPC logger with the specified options
        /// </summary>
        /// <param name="options">Logger configuration options</param>
        /// <param name="logger">Optional ILogger instance for integration with logging frameworks</param>
        public RpcLogger(RpcLoggerOptions options, ILogger? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public void Error(string message, object? data = null, Exception? exception = null)
        {
            if (_options.Level < RpcLogLevel.Error) return;
            Log(RpcLogLevel.Error, message, data, exception);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public void Warn(string message, object? data = null)
        {
            if (_options.Level < RpcLogLevel.Warn) return;
            Log(RpcLogLevel.Warn, message, data);
        }

        /// <summary>
        /// Log an informational message
        /// </summary>
        public void Info(string message, object? data = null)
        {
            if (_options.Level < RpcLogLevel.Info) return;
            Log(RpcLogLevel.Info, message, data);
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        public void Debug(string message, object? data = null)
        {
            if (_options.Level < RpcLogLevel.Debug) return;
            Log(RpcLogLevel.Debug, message, data);
        }

        /// <summary>
        /// Log a trace message
        /// </summary>
        public void Trace(string message, object? data = null)
        {
            if (_options.Level < RpcLogLevel.Trace) return;
            Log(RpcLogLevel.Trace, message, data);
        }

        /// <summary>
        /// Log a message at the specified level
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Log message</param>
        /// <param name="data">Optional structured data to include</param>
        /// <param name="exception">Optional exception to log</param>
        public void Log(RpcLogLevel level, string message, object? data = null, Exception? exception = null)
        {
            var entry = new LogEntry
            {
                Level = level.ToString().ToLower(),
                Message = message,
                Timestamp = _options.IncludeTimestamp ? DateTime.UtcNow : null,
                Data = data,
                Exception = exception != null ? new
                {
                    Type = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace
                } : null,
                Prefix = _options.Prefix
            };

            var output = _options.Format == RpcLogFormat.Json
                ? FormatJson(entry)
                : FormatText(entry);

            // Use ILogger if available
            if (_logger != null)
            {
                var logLevel = MapToLogLevel(level);
                if (exception != null)
                {
                    _logger.Log(logLevel, exception, output);
                }
                else
                {
                    _logger.Log(logLevel, output);
                }
            }
            else
            {
                // Fallback to console
                var color = GetConsoleColor(level);
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(output);
                Console.ForegroundColor = originalColor;
            }
        }

        private string FormatJson(LogEntry entry)
        {
            var obj = new Dictionary<string, object?>();

            if (entry.Timestamp.HasValue)
                obj["timestamp"] = entry.Timestamp.Value.ToString("o");

            if (!string.IsNullOrEmpty(entry.Prefix))
                obj["prefix"] = entry.Prefix;

            obj["level"] = entry.Level;
            obj["message"] = entry.Message;

            if (entry.Data != null)
                obj["data"] = entry.Data;

            if (entry.Exception != null)
                obj["exception"] = entry.Exception;

            return JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }

        private string FormatText(LogEntry entry)
        {
            var parts = new List<string>();

            if (entry.Timestamp.HasValue)
                parts.Add($"[{entry.Timestamp.Value:yyyy-MM-dd HH:mm:ss.fff}]");

            if (!string.IsNullOrEmpty(entry.Prefix))
                parts.Add($"[{entry.Prefix}]");

            parts.Add($"[{entry.Level.ToUpper()}]");
            parts.Add(entry.Message);

            if (entry.Data != null)
            {
                var dataJson = JsonSerializer.Serialize(entry.Data, new JsonSerializerOptions
                {
                    WriteIndented = false
                });
                parts.Add($"- {dataJson}");
            }

            if (entry.Exception != null)
            {
                parts.Add($"- Exception: {JsonSerializer.Serialize(entry.Exception)}");
            }

            return string.Join(" ", parts);
        }

        private static ConsoleColor GetConsoleColor(RpcLogLevel level)
        {
            return level switch
            {
                RpcLogLevel.Error => ConsoleColor.Red,
                RpcLogLevel.Warn => ConsoleColor.Yellow,
                RpcLogLevel.Info => ConsoleColor.Green,
                RpcLogLevel.Debug => ConsoleColor.Cyan,
                RpcLogLevel.Trace => ConsoleColor.Gray,
                _ => ConsoleColor.White
            };
        }

        private static LogLevel MapToLogLevel(RpcLogLevel level)
        {
            return level switch
            {
                RpcLogLevel.Error => LogLevel.Error,
                RpcLogLevel.Warn => LogLevel.Warning,
                RpcLogLevel.Info => LogLevel.Information,
                RpcLogLevel.Debug => LogLevel.Debug,
                RpcLogLevel.Trace => LogLevel.Trace,
                _ => LogLevel.None
            };
        }

        private class LogEntry
        {
            public string Level { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public DateTime? Timestamp { get; set; }
            public object? Data { get; set; }
            public object? Exception { get; set; }
            public string? Prefix { get; set; }
        }
    }
}
