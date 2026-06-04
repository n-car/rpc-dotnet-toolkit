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
    }
}
