using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using StyleCommerce.Api.Middleware;
using StyleCommerce.Api.Models;
using Xunit;

namespace StyleCommerce.Api.Tests
{
    public class SecurityHeadersMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_AddsSecurityHeaders()
        {
            var securitySettings = new SecuritySettings
            {
                AllowedCorsOrigins = new[] { "http://localhost:5173" },
            };

            var options = Options.Create(securitySettings);
            var nextMiddleware = new Mock<RequestDelegate>();
            nextMiddleware.Setup(next => next(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

            var middleware = new SecurityHeadersMiddleware(nextMiddleware.Object, options);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            var headers = context.Response.Headers;

            Assert.Equal("DENY", headers["X-Frame-Options"]);
            Assert.Equal("nosniff", headers["X-Content-Type-Options"]);
            Assert.Equal("1; mode=block", headers["X-XSS-Protection"]);
            Assert.Equal(
                "max-age=31536000; includeSubDomains",
                headers["Strict-Transport-Security"]
            );
            Assert.Equal("no-referrer", headers["Referrer-Policy"]);
            Assert.Equal("geolocation=(), microphone=(), camera=()", headers["Permissions-Policy"]);
            Assert.Equal("none", headers["X-Permitted-Cross-Domain-Policies"]);

            var csp = headers["Content-Security-Policy"].ToString();
            Assert.Contains("default-src 'self'", csp);
            Assert.Contains("script-src 'self' 'unsafe-inline'", csp);
            Assert.Contains("style-src 'self' 'unsafe-inline'", csp);
            Assert.Contains("img-src 'self' data: https:", csp);
            Assert.Contains("font-src 'self' data:", csp);
            Assert.Contains("connect-src 'self' http://localhost:5173", csp);
            Assert.Contains("frame-ancestors 'none'", csp);
            Assert.Contains("form-action 'self'", csp);
            Assert.Contains("base-uri 'self'", csp);
            Assert.Contains("object-src 'none'", csp);
            Assert.Contains("media-src 'self'", csp);
            Assert.Contains("frame-src https://js.stripe.com https://m.stripe.network", csp);
            Assert.Contains("child-src 'none'", csp);
            Assert.Contains("worker-src 'self'", csp);

            nextMiddleware.Verify(next => next(It.IsAny<HttpContext>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithMultipleAllowedOrigins_IncludesAllInCSP()
        {
            var securitySettings = new SecuritySettings
            {
                AllowedCorsOrigins = new[] { "http://localhost:5173", "https://localhost:5173" },
            };

            var options = Options.Create(securitySettings);
            var nextMiddleware = new Mock<RequestDelegate>();
            nextMiddleware.Setup(next => next(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

            var middleware = new SecurityHeadersMiddleware(nextMiddleware.Object, options);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            var csp = context.Response.Headers["Content-Security-Policy"].ToString();
            Assert.Contains("connect-src 'self' http://localhost:5173 https://localhost:5173", csp);
        }

        [Fact]
        public async Task InvokeAsync_WithNoAllowedOrigins_UsesDefault()
        {
            var securitySettings = new SecuritySettings
            {
                AllowedCorsOrigins = new string[0],
            };

            var options = Options.Create(securitySettings);
            var nextMiddleware = new Mock<RequestDelegate>();
            nextMiddleware.Setup(next => next(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

            var middleware = new SecurityHeadersMiddleware(nextMiddleware.Object, options);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            var csp = context.Response.Headers["Content-Security-Policy"].ToString();
            Assert.Contains("connect-src 'self' http://localhost:5173", csp);
        }
    }
}
