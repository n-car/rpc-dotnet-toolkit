using System;
using System.Threading.Tasks;
using Xunit;
using RpcToolkit;
using RpcToolkit.Exceptions;

namespace RpcToolkit.Tests
{
    public class RpcEndpointTests
    {
        [Fact]
        public void Constructor_InitializesSuccessfully()
        {
            // Arrange & Act
            var endpoint = new RpcEndpoint();

            // Assert
            Assert.NotNull(endpoint);
        }

        [Fact]
        public void AddMethod_RegistersMethod()
        {
            // Arrange
            var endpoint = new RpcEndpoint();

            // Act
            endpoint.AddMethod<int[], int>("add", (p, ctx) =>
            {
                return p![0] + p[1];
            });

            // Assert
            Assert.Contains("add", endpoint.GetMethods());
        }

        [Fact]
        public void AddMethod_ThrowsOnDuplicateMethod()
        {
            // Arrange
            var endpoint = new RpcEndpoint();
            endpoint.AddMethod<int[], int>("test", (p, ctx) => 42);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                endpoint.AddMethod<int[], int>("test", (p, ctx) => 99));
        }

        [Fact]
        public void RemoveMethod_RemovesRegisteredMethod()
        {
            // Arrange
            var endpoint = new RpcEndpoint();
            endpoint.AddMethod<int[], int>("test", (p, ctx) => 42);

            // Act
            endpoint.RemoveMethod("test");

            // Assert
            Assert.DoesNotContain("test", endpoint.GetMethods());
        }

        [Fact]
        public async Task HandleRequestAsync_ExecutesMethod()
        {
            // Arrange
            var endpoint = new RpcEndpoint();
            endpoint.AddMethod<AddParams, int>("add", (p, ctx) =>
            {
                return p!.A + p.B;
            });

            var request = @"{""jsonrpc"":""2.0"",""method"":""add"",""params"":{""a"":5,""b"":3},""id"":1}";

            // Act
            var response = await endpoint.HandleRequestAsync(request);

            // Assert
            Assert.Contains("\"result\":8", response);
            Assert.Contains("\"id\":1", response);
        }

        [Fact]
        public async Task HandleRequestAsync_ReturnsMethodNotFound()
        {
            // Arrange
            var endpoint = new RpcEndpoint();
            var request = @"{""jsonrpc"":""2.0"",""method"":""nonexistent"",""id"":1}";

            // Act
            var response = await endpoint.HandleRequestAsync(request);

            // Assert
            Assert.Contains("\"error\"", response);
            Assert.Contains("-32601", response); // Method not found
        }

        [Fact]
        public async Task HandleRequestAsync_ProcessesBatch()
        {
            // Arrange
            var endpoint = new RpcEndpoint();
            endpoint.AddMethod<AddParams, int>("add", (p, ctx) => p!.A + p.B);

            var request = @"[
                {""jsonrpc"":""2.0"",""method"":""add"",""params"":{""a"":1,""b"":2},""id"":1},
                {""jsonrpc"":""2.0"",""method"":""add"",""params"":{""a"":3,""b"":4},""id"":2}
            ]";

            // Act
            var response = await endpoint.HandleRequestAsync(request);

            // Assert
            Assert.Contains("\"result\":3", response);
            Assert.Contains("\"result\":7", response);
        }

        [Fact]
        public async Task HandleRequestAsync_HandlesNotification()
        {
            // Arrange
            var endpoint = new RpcEndpoint();
            var called = false;
            endpoint.AddMethod<object, string>("notify", (p, ctx) =>
            {
                called = true;
                return "ok";
            });

            var request = @"{""jsonrpc"":""2.0"",""method"":""notify""}";

            // Act
            var response = await endpoint.HandleRequestAsync(request);

            // Assert
            Assert.True(called);
            Assert.Empty(response); // Notifications return no response
        }

        [Fact]
        public async Task HandleRequestAsync_PassesContext()
        {
            // Arrange
            var contextData = new { UserId = 123 };
            var endpoint = new RpcEndpoint(contextData);
            
            object? capturedContext = null;
            endpoint.AddMethod<object, string>("test", (p, ctx) =>
            {
                capturedContext = ctx;
                return "ok";
            });

            var request = @"{""jsonrpc"":""2.0"",""method"":""test"",""id"":1}";

            // Act
            await endpoint.HandleRequestAsync(request);

            // Assert
            Assert.NotNull(capturedContext);
            Assert.Equal(contextData, capturedContext);
        }

        private class AddParams
        {
            public int A { get; set; }
            public int B { get; set; }
        }
    }
}
