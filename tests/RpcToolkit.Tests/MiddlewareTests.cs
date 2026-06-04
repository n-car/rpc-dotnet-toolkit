using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using RpcToolkit.Middleware;

namespace RpcToolkit.Tests
{
    public class MiddlewareTests
    {
        [Fact]
        public void MiddlewareManager_AddsMiddleware()
        {
            // Arrange
            var manager = new MiddlewareManager();
            var middleware = new TestMiddleware();

            // Act
            manager.Add(middleware, "before");

            // Assert
            Assert.Equal(1, manager.Count);
        }

        [Fact]
        public async Task MiddlewareManager_ExecutesBeforeMiddleware()
        {
            // Arrange
            var manager = new MiddlewareManager();
            var middleware = new TestMiddleware();
            manager.Add(middleware, "before");

            var request = new RpcRequest { Method = "test" };

            // Act
            await manager.ExecuteBeforeAsync(request, null);

            // Assert
            Assert.True(middleware.BeforeCalled);
        }

        [Fact]
        public async Task MiddlewareManager_ExecutesAfterMiddleware()
        {
            // Arrange
            var manager = new MiddlewareManager();
            var middleware = new TestMiddleware();
            manager.Add(middleware, "after");

            var request = new RpcRequest { Method = "test" };

            // Act
            await manager.ExecuteAfterAsync(request, "result", null);

            // Assert
            Assert.True(middleware.AfterCalled);
        }

        [Fact]
        public async Task RateLimitMiddleware_EnforcesLimit()
        {
            // Arrange
            var middleware = new RateLimitMiddleware(2, 60, "global");
            var request = new RpcRequest { Method = "test" };

            // Act & Assert - first two should succeed
            await middleware.BeforeAsync(request, null);
            await middleware.BeforeAsync(request, null);

            // Third should throw
            await Assert.ThrowsAsync<RpcToolkit.Exceptions.RateLimitExceededException>(
                async () => await middleware.BeforeAsync(request, null));
        }

        [Fact]
        public void CorsMiddleware_Instantiates()
        {
            // Arrange & Act
            var middleware = new CorsMiddleware();

            // Assert
            Assert.NotNull(middleware);
        }

        [Fact]
        public async Task AuthMiddleware_RequiresAuthentication()
        {
            // Arrange
            var middleware = new AuthMiddleware(
                token => token == "valid" ? new { UserId = 123 } : null,
                required: true
            );

            var request = new RpcRequest { Method = "test" };

            // Act & Assert
            await Assert.ThrowsAsync<RpcToolkit.Exceptions.AuthenticationErrorException>(
                async () => await middleware.BeforeAsync(request, null));
        }

        [Fact]
        public async Task AuthMiddleware_AuthenticatesBearerTokenFromRpcRequestContext()
        {
            // Arrange
            var middleware = new AuthMiddleware(
                token => token == "valid" ? new { UserId = 123 } : null,
                required: true
            );

            var request = new RpcRequest { Method = "test" };
            var context = new RpcRequestContext
            {
                Authorization = "Bearer valid"
            };

            // Act
            await middleware.BeforeAsync(request, context);

            // Assert
            Assert.NotNull(context.User);
        }

        [Fact]
        public async Task AuthMiddleware_ReadsAuthorizationFromHeaders()
        {
            // Arrange
            var middleware = new AuthMiddleware(
                token => token == "valid" ? new { UserId = 123 } : null,
                required: true
            );

            var request = new RpcRequest { Method = "test" };
            var context = new RpcRequestContext(new Dictionary<string, string>
            {
                ["authorization"] = "Bearer valid"
            });

            // Act
            await middleware.BeforeAsync(request, context);

            // Assert
            Assert.NotNull(context.User);
        }

        [Fact]
        public async Task AuthMiddleware_UsesAuthenticatedContextUserWithoutToken()
        {
            // Arrange
            var middleware = new AuthMiddleware(
                token => throw new InvalidOperationException("Token authenticator should not run"),
                required: true
            );

            var request = new RpcRequest { Method = "test" };
            var user = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "browser-user") },
                "Cookie"));
            var context = new RpcRequestContext
            {
                User = user
            };

            // Act
            await middleware.BeforeAsync(request, context);

            // Assert
            Assert.Equal(user, context.User);
        }

        [Fact]
        public async Task AuthMiddleware_RejectsUnauthenticatedPrincipalWithoutToken()
        {
            // Arrange
            var middleware = new AuthMiddleware(
                token => token == "valid" ? new { UserId = 123 } : null,
                required: true
            );

            var request = new RpcRequest { Method = "test" };
            var context = new RpcRequestContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            };

            // Act & Assert
            await Assert.ThrowsAsync<RpcToolkit.Exceptions.AuthenticationErrorException>(
                async () => await middleware.BeforeAsync(request, context));
        }

        private class TestMiddleware : IMiddleware
        {
            public bool BeforeCalled { get; private set; }
            public bool AfterCalled { get; private set; }

            public Task BeforeAsync(RpcRequest request, object? context)
            {
                BeforeCalled = true;
                return Task.CompletedTask;
            }

            public Task AfterAsync(RpcRequest request, object? result, object? context)
            {
                AfterCalled = true;
                return Task.CompletedTask;
            }
        }
    }
}
