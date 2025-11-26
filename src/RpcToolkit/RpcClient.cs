using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

            _logger?.LogInformation("RpcClient initialized for {BaseUrl}", _baseUrl);
        }

        /// <summary>
        /// Set authentication token
        /// </summary>
        public void SetAuthToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
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
            var rpcResponse = SerializerFactory.Deserialize<RpcResponse>(response, _options.SafeEnabled);

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
            var resultJson = SerializerFactory.Serialize(rpcResponse.Result, _options.SafeEnabled);
            return SerializerFactory.Deserialize<TResult>(resultJson, _options.SafeEnabled);
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
            var rpcResponses = SerializerFactory.Deserialize<List<RpcResponse>>(response, _options.SafeEnabled);

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
        private async Task<string> SendRequestAsync(string jsonRequest)
        {
            try
            {
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(_baseUrl, content);

                httpResponse.EnsureSuccessStatusCode();

                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                
                // Notifications may return empty response
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    return "{}";
                }

                return responseContent;
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
                RpcErrorCodes.RateLimitExceeded => new RateLimitExceededException(error.Message, error.Data),
                RpcErrorCodes.ValidationError => new ValidationErrorException(error.Message, error.Data),
                _ => new ServerErrorException(error.Message, error.Data)
            };
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
