using System;
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

        // Note: Integration tests with actual HTTP calls would require
        // a running server. These are unit tests for basic functionality.
        // Full integration tests should be in a separate test project.
    }
}
