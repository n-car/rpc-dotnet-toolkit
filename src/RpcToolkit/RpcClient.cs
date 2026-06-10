using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpcToolkit.Exceptions;
using RpcToolkit.Serialization;

namespace RpcToolkit
{
    /// <summary>
    /// JSON-RPC 2.0 client
    /// </summary>
    public class RpcClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly RpcClientOptions _options;
        private readonly ILogger? _logger;
        private readonly bool _ownsHttpClient;
        private int _requestIdCounter = 1;

        /// <summary>
        /// Create a new RPC client
        /// </summary>
        /// <param name="baseUrl">Base URL of the RPC endpoint</param>
        /// <param name="options">Client options</param>
        /// <param name="logger">Optional logger</param>
        /// <param name="httpClient">Optional HttpClient instance (if null, creates one)</param>
        public RpcClient(
            string baseUrl,
            RpcClientOptions? options = null,
            ILogger? logger = null,
            HttpClient? httpClient = null)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Base URL cannot be empty", nameof(baseUrl));

            _baseUrl = baseUrl.TrimEnd('/');
            _options = options ?? new RpcClientOptions();
            _logger = logger;

            if (httpClient != null)
            {
                _httpClient = httpClient;
                _ownsHttpClient = false;
            }
            else
            {
                var handler = new HttpClientHandler();
                if (!_options.VerifySSL)
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                }

                _httpClient = new HttpClient(handler)
                {
                    Timeout = _options.Timeout
                };
                _ownsHttpClient = true;
            }

            // Set custom headers
            foreach (var header in _options.Headers)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            if (!_options.Headers.Keys.Any(key => string.Equals(key, "X-RPC-Safe-Enabled", StringComparison.OrdinalIgnoreCase)))
            {
                _httpClient.DefaultRequestHeaders.Add("X-RPC-Safe-Enabled", _options.SafeEnabled ? "true" : "false");
            }

            _logger?.LogInformation("RpcClient initialized for {BaseUrl}", _baseUrl);
        }

        /// <summary>
        /// Set authentication token
        /// </summary>
        public void SetAuthToken(string token, string scheme = "Bearer")
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Authentication token cannot be empty", nameof(token));

            if (string.IsNullOrWhiteSpace(scheme))
                throw new ArgumentException("Authentication scheme cannot be empty", nameof(scheme));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, token);
        }

        /// <summary>
        /// Clear authentication token
        /// </summary>
        public void ClearAuthToken()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        /// <summary>
        /// Make a single RPC call
        /// </summary>
        public async Task<TResult?> CallAsync<TResult>(string method, object? parameters = null)
        {
            var requestId = _requestIdCounter++;
            var request = new RpcRequest(method, parameters, requestId);
            
            var jsonRequest = SerializerFactory.Serialize(request, _options.SafeEnabled);
            _logger?.LogDebug("Calling method {Method} with ID {RequestId}", method, requestId);

            var response = await SendRequestAsync(jsonRequest);
            var responseSafeEnabled = response.SafeEnabled ?? _options.SafeEnabled;
            var rpcResponse = SerializerFactory.Deserialize<RpcResponse>(response.Content, responseSafeEnabled);

            if (rpcResponse == null)
            {
                throw new InvalidRequestException("Invalid response from server");
            }

            if (rpcResponse.HasError)
            {
                throw CreateExceptionFromError(rpcResponse.Error!);
            }

            if (rpcResponse.Result == null)
            {
                return default;
            }

            // Convert result to target type
            var resultJson = SerializerFactory.Serialize(rpcResponse.Result, responseSafeEnabled);
            return SerializerFactory.Deserialize<TResult>(resultJson, responseSafeEnabled);
        }

        /// <summary>
        /// Make a batch of RPC calls
        /// </summary>
        public async Task<List<RpcResponse>> BatchAsync(IEnumerable<RpcRequest> requests)
        {
            if (requests == null || !requests.Any())
            {
                throw new ArgumentException("Batch requests cannot be empty", nameof(requests));
            }

            var requestList = requests.ToList();
            var jsonRequest = SerializerFactory.Serialize(requestList, _options.SafeEnabled);
            
            _logger?.LogDebug("Sending batch request with {Count} calls", requestList.Count);

            var response = await SendRequestAsync(jsonRequest);
            if (string.IsNullOrWhiteSpace(response.Content))
            {
                return new List<RpcResponse>();
            }

            var responseSafeEnabled = response.SafeEnabled ?? _options.SafeEnabled;
            var rpcResponses = SerializerFactory.Deserialize<List<RpcResponse>>(response.Content, responseSafeEnabled);

            if (rpcResponses == null)
            {
                throw new InvalidRequestException("Invalid batch response from server");
            }

            return rpcResponses;
        }

        /// <summary>
        /// Send a notification (no response expected)
        /// </summary>
        public async Task NotifyAsync(string method, object? parameters = null)
        {
            var request = new RpcRequest(method, parameters, null); // null ID = notification
            var jsonRequest = SerializerFactory.Serialize(request, _options.SafeEnabled);
            
            _logger?.LogDebug("Sending notification {Method}", method);

            await SendRequestAsync(jsonRequest);
        }

        /// <summary>
        /// Send raw JSON-RPC request
        /// </summary>
        private async Task<RpcHttpResponse> SendRequestAsync(string jsonRequest)
        {
            try
            {
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(_baseUrl, content);

                httpResponse.EnsureSuccessStatusCode();

                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                var safeEnabled = ReadSafeModeHeader(httpResponse);
                
                return new RpcHttpResponse(responseContent, safeEnabled);
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP error calling RPC endpoint");
                throw new InternalErrorException($"HTTP error: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger?.LogError(ex, "Request timeout");
                throw new InternalErrorException("Request timeout", ex);
            }
        }

        private static bool? ReadSafeModeHeader(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("X-RPC-Safe-Enabled", out var values) ||
                response.Content.Headers.TryGetValues("X-RPC-Safe-Enabled", out values))
            {
                var value = values.FirstOrDefault();
                if (bool.TryParse(value, out var safeEnabled))
                {
                    return safeEnabled;
                }
            }

            return null;
        }

        /// <summary>
        /// Create appropriate exception from RPC error
        /// </summary>
        private static RpcException CreateExceptionFromError(RpcError error)
        {
            return error.Code switch
            {
                RpcErrorCodes.ParseError => new ParseErrorException(error.Message, error.Data),
                RpcErrorCodes.InvalidRequest => new InvalidRequestException(error.Message, error.Data),
                RpcErrorCodes.MethodNotFound => new MethodNotFoundException(error.Message),
                RpcErrorCodes.InvalidParams => new InvalidParamsException(error.Message, error.Data),
                RpcErrorCodes.InternalError => new InternalErrorException(error.Message, error.Data),
                RpcErrorCodes.AuthenticationError => new AuthenticationErrorException(error.Message, error.Data),
                RpcErrorCodes.AuthorizationError => new AuthorizationErrorException(error.Message, error.Data),
                RpcErrorCodes.RateLimitExceeded => new RateLimitExceededException(error.Message, error.Data),
                RpcErrorCodes.ValidationError => new ValidationErrorException(error.Message, error.Data),
                _ => new RemoteRpcException(error.Code, error.Message, error.Data)
            };
        }

        private sealed class RpcHttpResponse
        {
            public RpcHttpResponse(string content, bool? safeEnabled)
            {
                Content = content;
                SafeEnabled = safeEnabled;
            }

            public string Content { get; }
            public bool? SafeEnabled { get; }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (_ownsHttpClient)
            {
                _httpClient?.Dispose();
            }
        }
    }
}
