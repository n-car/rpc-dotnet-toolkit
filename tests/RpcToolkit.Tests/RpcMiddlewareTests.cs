using Microsoft.AspNetCore.Http;
using RpcToolkit.AspNetCore;
using Xunit;

namespace RpcToolkit.Tests
{
    public class RpcMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_HandlesCorsPreflightAtRpcPath()
        {
            var nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            var middleware = new RpcMiddleware(
                next,
                new RpcEndpoint(),
                new RpcMiddlewareOptions
                {
                    Path = "/rpc",
                    EnableCors = true,
                    AllowedOrigins = new[] { "https://app.example" }
                });
            var context = new DefaultHttpContext();
            context.Request.Method = "OPTIONS";
            context.Request.Path = "/rpc";
            context.Request.Headers.Origin = "https://app.example";
            context.Request.Headers.AccessControlRequestHeaders = "Content-Type, X-RPC-Safe-Enabled";

            await middleware.InvokeAsync(context);

            Assert.False(nextCalled);
            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
            Assert.Equal(
                "https://app.example",
                context.Response.Headers.AccessControlAllowOrigin.ToString());
            Assert.Contains("X-RPC-Safe-Enabled", context.Response.Headers.AccessControlAllowHeaders.ToString());
            Assert.Contains("X-RPC-Safe-Enabled", context.Response.Headers.AccessControlExposeHeaders.ToString());
        }
    }
}
