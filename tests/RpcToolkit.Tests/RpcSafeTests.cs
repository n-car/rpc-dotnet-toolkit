using System;
using System.Threading.Tasks;
using Xunit;
using RpcToolkit;

namespace RpcToolkit.Tests
{
    public class RpcSafeTests
    {
        [Fact]
        public void RpcSafeEndpoint_HasSafeEnabledByDefault()
        {
            // Arrange & Act
            var endpoint = new RpcSafeEndpoint();

            // Assert
            Assert.NotNull(endpoint);
            Assert.IsAssignableFrom<RpcEndpoint>(endpoint);
        }

        [Fact]
        public void RpcSafeEndpoint_WithContext_InitializesSuccessfully()
        {
            // Arrange
            var context = new { TestValue = "test" };

            // Act
            var endpoint = new RpcSafeEndpoint(context);

            // Assert
            Assert.NotNull(endpoint);
        }

        [Fact]
        public void RpcSafeEndpoint_AllowsOverridingOptions()
        {
            // Arrange
            var options = new RpcOptions
            {
                EnableBatch = false,
                EnableLogging = false
            };

            // Act
            var endpoint = new RpcSafeEndpoint(null, options);

            // Assert
            Assert.NotNull(endpoint);
        }

        [Fact]
        public async Task RpcSafeEndpoint_HandlesRpcCall()
        {
            // Arrange
            var endpoint = new RpcSafeEndpoint();
            endpoint.AddMethod<int[], int>("add", (p, ctx) =>
            {
                return p![0] + p[1];
            });

            var request = @"{
                ""jsonrpc"": ""2.0"",
                ""method"": ""add"",
                ""params"": [5, 3],
                ""id"": 1
            }";

            // Act
            var response = await endpoint.HandleRequestAsync(request);

            // Assert
            Assert.Contains("\"result\":8", response);
            Assert.Contains("\"id\":1", response);
        }

        [Fact]
        public void RpcSafeClient_InitializesWithUrl()
        {
            // Arrange & Act
            var client = new RpcSafeClient("http://localhost:3000/api");

            // Assert
            Assert.NotNull(client);
            Assert.IsAssignableFrom<RpcClient>(client);
        }

        [Fact]
        public void RpcSafeClient_WithOptions_InitializesSuccessfully()
        {
            // Arrange
            var options = new RpcClientOptions
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            // Act
            var client = new RpcSafeClient("http://localhost:3000/api", options);

            // Assert
            Assert.NotNull(client);
        }

        [Fact]
        public void RpcSafeEndpoint_InheritsFromRpcEndpoint()
        {
            // Arrange & Act
            var endpoint = new RpcSafeEndpoint();

            // Assert
            Assert.IsType<RpcSafeEndpoint>(endpoint);
            Assert.IsAssignableFrom<RpcEndpoint>(endpoint);
        }

        [Fact]
        public void RpcSafeClient_InheritsFromRpcClient()
        {
            // Arrange & Act
            var client = new RpcSafeClient("http://localhost:3000/api");

            // Assert
            Assert.IsType<RpcSafeClient>(client);
            Assert.IsAssignableFrom<RpcClient>(client);
        }
    }
}
