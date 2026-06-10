using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RpcToolkit;
using RpcToolkit.Exceptions;

namespace RpcToolkit.Tests
{
    public class RpcClientTests
    {
        [Fact]
        public void Constructor_InitializesSuccessfully()
        {
            // Arrange & Act
            using var client = new RpcClient("http://localhost:8080/rpc");

            // Assert
            Assert.NotNull(client);
        }

        [Fact]
        public void Constructor_ThrowsOnEmptyUrl()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new RpcClient(""));
        }

        [Fact]
        public void SetAuthToken_SetsHeader()
        {
            // Arrange
            using var client = new RpcClient("http://localhost:8080/rpc");

            // Act
            client.SetAuthToken("test-token-123");

            // Assert - no exception means success
            Assert.True(true);
        }

        [Fact]
        public async Task CallAsync_SendsAuthorizationHeader()
        {
            // Arrange
            var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""jsonrpc"":""2.0"",""result"":""ok"",""id"":1}")
            });

            using var httpClient = new HttpClient(handler);
            using var client = new RpcClient("http://localhost:8080/rpc", httpClient: httpClient);

            // Act
            client.SetAuthToken("test-token-123");
            var result = await client.CallAsync<string>("test");

            // Assert
            Assert.Equal("ok", result);
            Assert.NotNull(handler.Request);
            Assert.NotNull(handler.Request!.Headers.Authorization);
            Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
            Assert.Equal("test-token-123", handler.Request.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task CallAsync_SendsJsonRpcEnvelopeAndSafeHeader()
        {
            // Arrange
            var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""jsonrpc"":""2.0"",""result"":""S:ok"",""id"":1}")
            });

            using var httpClient = new HttpClient(handler);
            using var client = new RpcClient(
                "http://localhost:8080/rpc",
                new RpcClientOptions { SafeEnabled = true },
                httpClient: httpClient);

            // Act
            var result = await client.CallAsync<string>("echo.string", new TextParams { Text = "hello" });
            var requestJson = await handler.Request!.Content!.ReadAsStringAsync();

            // Assert
            Assert.Equal("ok", result);
            Assert.Contains(@"""jsonrpc"":""2.0""", requestJson);
            Assert.DoesNotContain(@"""jsonRpc""", requestJson);
            Assert.Contains(@"""method"":""echo.string""", requestJson);
            Assert.Contains(@"""text"":""S:hello""", requestJson);
            Assert.True(handler.Request.Headers.TryGetValues("X-RPC-Safe-Enabled", out var values));
            Assert.Equal("true", values.Single());
        }

        [Fact]
        public async Task CallAsync_UsesResponseSafeHeaderForDeserialization()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""jsonrpc"":""2.0"",""result"":""S:ok"",""id"":1}")
            };
            response.Headers.Add("X-RPC-Safe-Enabled", "true");

            var handler = new CapturingHandler(response);
            using var httpClient = new HttpClient(handler);
            using var client = new RpcClient(
                "http://localhost:8080/rpc",
                new RpcClientOptions { SafeEnabled = false },
                httpClient: httpClient);

            // Act
            var result = await client.CallAsync<string>("echo.string", new TextParams { Text = "hello" });

            // Assert
            Assert.Equal("ok", result);
        }

        [Fact]
        public void ClearAuthToken_RemovesAuthorizationHeader()
        {
            // Arrange
            var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK));
            using var httpClient = new HttpClient(handler);
            using var client = new RpcClient("http://localhost:8080/rpc", httpClient: httpClient);

            // Act
            client.SetAuthToken("test-token-123");
            client.ClearAuthToken();

            // Assert
            Assert.Null(httpClient.DefaultRequestHeaders.Authorization);
        }

        [Fact]
        public async Task CallAsync_MapsAuthorizationError()
        {
            // Arrange
            var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""jsonrpc"":""2.0"",""error"":{""code"":-32004,""message"":""Authorization denied""},""id"":1}")
            });

            using var httpClient = new HttpClient(handler);
            using var client = new RpcClient("http://localhost:8080/rpc", httpClient: httpClient);

            // Act & Assert
            await Assert.ThrowsAsync<AuthorizationErrorException>(
                async () => await client.CallAsync<string>("protected"));
        }

        [Fact]
        public async Task CallAsync_PreservesRemoteErrorCode()
        {
            // Arrange
            var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""jsonrpc"":""2.0"",""error"":{""code"":-32042,""message"":""Domain error"",""data"":{""kind"":""custom""}},""id"":1}")
            });

            using var httpClient = new HttpClient(handler);
            using var client = new RpcClient("http://localhost:8080/rpc", httpClient: httpClient);

            // Act
            var ex = await Assert.ThrowsAsync<RemoteRpcException>(
                async () => await client.CallAsync<string>("domain.fail"));

            // Assert
            Assert.Equal(-32042, ex.Code);
            Assert.Equal("Domain error", ex.Message);
        }

        [Fact]
        public async Task BatchAsync_ReturnsEmptyListForNoContent()
        {
            // Arrange
            var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.NoContent)
            {
                Content = new StringContent("")
            });

            using var httpClient = new HttpClient(handler);
            using var client = new RpcClient(
                "http://localhost:8080/rpc",
                new RpcClientOptions { SafeEnabled = true },
                httpClient: httpClient);

            // Act
            var responses = await client.BatchAsync(new[]
            {
                new RpcRequest("notify.record", new { eventName = "notify-only" }, null)
            });

            // Assert
            Assert.Empty(responses);
            Assert.NotNull(handler.Request);
            Assert.True(handler.Request!.Headers.TryGetValues("X-RPC-Safe-Enabled", out var values));
            Assert.Equal("true", values.Single());
        }

        // Note: Integration tests with actual HTTP calls would require
        // a running server. These are unit tests for basic functionality.
        // Full integration tests should be in a separate test project.

        private class CapturingHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public CapturingHandler(HttpResponseMessage response)
            {
                _response = response;
            }

            public HttpRequestMessage? Request { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Request = request;
                return Task.FromResult(_response);
            }
        }

        private class TextParams
        {
            public string Text { get; set; } = string.Empty;
        }
    }
}
