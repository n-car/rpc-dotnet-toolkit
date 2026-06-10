using System;
using System.Collections.Generic;
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
        public async Task RpcSafeEndpoint_RequiresSafeHeaderForHttpContext()
        {
            // Arrange
            var endpoint = new RpcSafeEndpoint(null, new RpcOptions { EnableLogging = false });
            endpoint.AddMethod<object, string>("ping", (_, __) => "pong");

            var request = @"{""jsonrpc"":""2.0"",""method"":""ping"",""id"":1}";
            var context = new RpcRequestContext(new Dictionary<string, string>());

            // Act
            var response = await endpoint.HandleRequestAsync(request, context);

            // Assert
            Assert.Contains(@"""error"":", response);
            Assert.Contains("-32600", response);
            Assert.Contains("X-RPC-Safe-Enabled", response);
        }

        [Fact]
        public async Task RpcSafeEndpoint_AllowsSafeHeaderForHttpContext()
        {
            // Arrange
            var endpoint = new RpcSafeEndpoint(null, new RpcOptions { EnableLogging = false });
            endpoint.AddMethod<object, string>("ping", (_, __) => "pong");

            var request = @"{""jsonrpc"":""2.0"",""method"":""ping"",""id"":1}";
            var context = new RpcRequestContext(new Dictionary<string, string>
            {
                ["X-RPC-Safe-Enabled"] = "true"
            });

            // Act
            var response = await endpoint.HandleRequestAsync(request, context);

            // Assert
            Assert.Contains(@"""result"":""S:pong""", response);
            Assert.Contains(@"""id"":1", response);
        }

        [Fact]
        public async Task RpcSafeEndpoint_FiltersBatchNotifications()
        {
            // Arrange
            var notificationCount = 0;
            var endpoint = new RpcSafeEndpoint(null, new RpcOptions { EnableLogging = false });
            endpoint.AddMethod<object, string>("ping", (_, __) => "pong");
            endpoint.AddMethod<object, string>("notify", (_, __) =>
            {
                notificationCount++;
                return "notified";
            });

            var request = @"[
                {""jsonrpc"":""2.0"",""method"":""ping"",""id"":1},
                {""jsonrpc"":""2.0"",""method"":""notify""}
            ]";

            // Act
            var response = await endpoint.HandleRequestAsync(request);

            // Assert
            Assert.Equal(1, notificationCount);
            Assert.Contains(@"""result"":""S:pong""", response);
            Assert.DoesNotContain("notified", response);
            Assert.DoesNotContain(@"""id"":null", response);
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
