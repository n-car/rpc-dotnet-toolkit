using System;
using System.Collections.Generic;

namespace RpcToolkit
{
    /// <summary>
    /// Options for batch request processing
    /// </summary>
    public class RpcBatchOptions
    {
        /// <summary>
        /// Maximum number of requests in a single batch
        /// </summary>
        public int MaxSize { get; set; } = 100;

        /// <summary>
        /// Execute batch requests in parallel
        /// </summary>
        public bool Parallel { get; set; } = true;

        /// <summary>
        /// Maximum degree of parallelism (0 = unlimited)
        /// </summary>
        public int MaxParallelism { get; set; } = 10;

        /// <summary>
        /// Continue processing remaining requests if one fails
        /// </summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>
        /// Timeout for the entire batch operation (in seconds, 0 = no timeout)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 0;

        /// <summary>
        /// Enable batch request metrics/statistics
        /// </summary>
        public bool CollectMetrics { get; set; } = false;
    }

    /// <summary>
    /// Metrics collected during batch execution
    /// </summary>
    public class RpcBatchMetrics
    {
        /// <summary>Total requests in batch</summary>
        public int TotalRequests { get; set; }

        /// <summary>Successfully processed requests</summary>
        public int SuccessCount { get; set; }

        /// <summary>Failed requests</summary>
        public int ErrorCount { get; set; }

        /// <summary>Total execution time in milliseconds</summary>
        public long TotalDurationMs { get; set; }

        /// <summary>Average request duration in milliseconds</summary>
        public double AverageDurationMs { get; set; }

        /// <summary>Minimum request duration in milliseconds</summary>
        public long MinDurationMs { get; set; }

        /// <summary>Maximum request duration in milliseconds</summary>
        public long MaxDurationMs { get; set; }

        /// <summary>Timestamp when batch started</summary>
        public DateTime StartTime { get; set; }

        /// <summary>Timestamp when batch completed</summary>
        public DateTime EndTime { get; set; }

        /// <summary>Individual request timings</summary>
        public List<RequestTiming> RequestTimings { get; set; } = new();
    }

    /// <summary>
    /// Timing information for a single request in a batch
    /// </summary>
    public class RequestTiming
    {
        /// <summary>Request ID</summary>
        public object? Id { get; set; }

        /// <summary>Method name</summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>Duration in milliseconds</summary>
        public long DurationMs { get; set; }

        /// <summary>Whether the request succeeded</summary>
        public bool Success { get; set; }

        /// <summary>Error message if failed</summary>
        public string? ErrorMessage { get; set; }
    }
}
