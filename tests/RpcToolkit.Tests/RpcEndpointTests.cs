using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using RpcToolkit;
using RpcToolkit.Exceptions;
using RpcToolkit.Middleware;

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
        public async Task HandleRequestAsync_ProcessesBatchWhenLoggingIsDisabled()
        {
            // Arrange
            var endpoint = new RpcEndpoint(options: new RpcOptions
            {
                EnableLogging = false
            });
            endpoint.AddMethod<AddParams, int>("add", (p, ctx) => p!.A + p.B);

            var request = @"[
                {""jsonrpc"":""2.0"",""method"":""add"",""params"":{""a"":7,""b"":8},""id"":1}
            ]";

            // Act
            var response = await endpoint.HandleRequestAsync(request);

            // Assert
            Assert.Contains("\"jsonrpc\":\"2.0\"", response);
            Assert.DoesNotContain("\"jsonRpc\"", response);
            Assert.Contains("\"result\":15", response);
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

        [Fact]
        public async Task HandleRequestAsync_PassesPerRequestContextToMiddlewareAndHandler()
        {
            // Arrange
            var endpoint = new RpcEndpoint();
            endpoint.GetMiddleware()?.Add(new AuthMiddleware(
                token => token == "valid" ? new { UserId = 123 } : null,
                required: true
            ));

            endpoint.AddMethod<object, string>("protected", (p, ctx) =>
            {
                var requestContext = Assert.IsType<RpcRequestContext>(ctx);
                Assert.NotNull(requestContext.User);
                return "ok";
            });

            var request = @"{""jsonrpc"":""2.0"",""method"":""protected"",""id"":1}";
            var context = new RpcRequestContext
            {
                Authorization = "Bearer valid"
            };

            // Act
            var response = await endpoint.HandleRequestAsync(request, context);

            // Assert
            Assert.Contains("\"result\":\"ok\"", response);
        }

        [Fact]
        public async Task HandleRequestAsync_UsesAuthenticatedContextUserWithoutAuthorizationHeader()
        {
            // Arrange
            var endpoint = new RpcEndpoint();
            endpoint.GetMiddleware()?.Add(new AuthMiddleware(
                token => throw new InvalidOperationException("Token authenticator should not run"),
                required: true
            ));

            endpoint.AddMethod<object, string>("browser.protected", (p, ctx) =>
            {
                var requestContext = Assert.IsType<RpcRequestContext>(ctx);
                var principal = Assert.IsType<ClaimsPrincipal>(requestContext.User);
                return principal.Identity!.Name!;
            });

            var request = @"{""jsonrpc"":""2.0"",""method"":""browser.protected"",""id"":1}";
            var context = new RpcRequestContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Name, "browser-user") },
                    "Cookie"))
            };

            // Act
            var response = await endpoint.HandleRequestAsync(request, context);

            // Assert
            Assert.Contains("\"result\":\"browser-user\"", response);
        }

        [Fact]
        public async Task HandleRequestAsync_AllowsMethodWhenRequiredScopeIsPresent()
        {
            // Arrange
            var endpoint = new RpcEndpoint();
            endpoint.AddMethod<object, string>("modules.enable", (p, ctx) => "enabled", new MethodConfig
            {
                RequiredScopes = new[] { "modules.write" }
            });

            var request = @"{""jsonrpc"":""2.0"",""method"":""modules.enable"",""id"":1}";
            var context = new RpcRequestContext
            {
                Principal = CreatePrincipal(new Claim("scope", "modules.read modules.write"))
            };

            // Act
            var response = await endpoint.HandleRequestAsync(request, context);

            // Assert
            Assert.Contains("\"result\":\"enabled\"", response);
        }

        [Fact]
        public async Task HandleRequestAsync_DeniesMethodWhenRequiredScopeIsMissing()
        {
            // Arrange
            var endpoint = new RpcEndpoint();
            var called = false;
            endpoint.AddMethod<object, string>("modules.enable", (p, ctx) =>
            {
                called = true;
                return "enabled";
            }, new MethodConfig
            {
                RequiredScopes = new[] { "modules.write" }
            });

            var request = @"{""jsonrpc"":""2.0"",""method"":""modules.enable"",""id"":1}";
            var context = new RpcRequestContext
            {
                Principal = CreatePrincipal(new Claim("scope", "modules.read"))
            };

            // Act
            var response = await endpoint.HandleRequestAsync(request, context);

            // Assert
            Assert.False(called);
            Assert.Contains("\"error\"", response);
            Assert.Contains("-32004", response);
        }

        [Fact]
        public async Task HandleRequestAsync_ReturnsAuthenticationErrorWhenPolicyRequiresPrincipal()
        {
            // Arrange
            var endpoint = new RpcEndpoint();
            endpoint.AddMethod<object, string>("modules.list", (p, ctx) => "[]", new MethodConfig
            {
                RequiredScopes = new[] { "modules.read" }
            });

            var request = @"{""jsonrpc"":""2.0"",""method"":""modules.list"",""id"":1}";

            // Act
            var response = await endpoint.HandleRequestAsync(request, new RpcRequestContext());

            // Assert
            Assert.Contains("\"error\"", response);
            Assert.Contains("-32001", response);
        }

        [Fact]
        public async Task HandleRequestAsync_AllowsMethodWhenRequiredRoleIsPresent()
        {
            // Arrange
            var endpoint = new RpcEndpoint();
            endpoint.AddMethod<object, string>("runtime.admin", (p, ctx) => "ok", new MethodConfig
            {
                RequiredRoles = new[] { "Admin" }
            });

            var request = @"{""jsonrpc"":""2.0"",""method"":""runtime.admin"",""id"":1}";
            var context = new RpcRequestContext
            {
                Principal = CreatePrincipal(new Claim(ClaimTypes.Role, "Admin"))
            };

            // Act
            var response = await endpoint.HandleRequestAsync(request, context);

            // Assert
            Assert.Contains("\"result\":\"ok\"", response);
        }

        [Fact]
        public async Task HandleRequestAsync_UsesAsyncAuthorizationPolicy()
        {
            // Arrange
            var endpoint = new RpcEndpoint();
            endpoint.AddMethod<object, string>("tray.status", (p, ctx) => "online", new MethodConfig
            {
                AuthorizeAsync = authContext =>
                {
                    Assert.Equal("tray.status", authContext.Request.Method);
                    Assert.Equal("tray.status", authContext.Method.Name);
                    Assert.NotNull(authContext.Context);
                    Assert.Equal("tray", authContext.Principal!.Identity!.Name);
                    return Task.FromResult(authContext.Principal.HasClaim("scope", "tray.status"));
                }
            });

            var request = @"{""jsonrpc"":""2.0"",""method"":""tray.status"",""id"":1}";
            var context = new RpcRequestContext
            {
                Principal = CreatePrincipal(
                    new Claim(ClaimTypes.Name, "tray"),
                    new Claim("scope", "tray.status"))
            };

            // Act
            var response = await endpoint.HandleRequestAsync(request, context);

            // Assert
            Assert.Contains("\"result\":\"online\"", response);
        }

        [Fact]
        public void GetMethod_ReturnsAuthorizationMetadata()
        {
            // Arrange
            var endpoint = new RpcEndpoint();
            endpoint.AddMethod<object, string>("modules.list", (p, ctx) => "[]", new MethodConfig
            {
                RequiredScopes = new[] { "modules.read" },
                RequiredRoles = new[] { "Admin" }
            });

            // Act
            var config = endpoint.GetMethod("modules.list");

            // Assert
            Assert.NotNull(config);
            Assert.Equal(new[] { "modules.read" }, config!.RequiredScopes);
            Assert.Equal(new[] { "Admin" }, config.RequiredRoles);
        }

        private class AddParams
        {
            public int A { get; set; }
            public int B { get; set; }
        }

        private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }
    }
}
