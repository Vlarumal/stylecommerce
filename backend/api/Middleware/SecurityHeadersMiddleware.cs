using Microsoft.Extensions.Options;
using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SecuritySettings _securitySettings;

        public SecurityHeadersMiddleware(
            RequestDelegate next,
            IOptions<SecuritySettings> securitySettings
        )
        {
            _next = next;
            _securitySettings = securitySettings.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var allowedOrigins = _securitySettings.AllowedCorsOrigins ?? new string[0];
            var allowedOrigin =
                allowedOrigins.Length > 0
                    ? string.Join(" ", allowedOrigins)
                    : "http://localhost:5173";

            // Security headers based on best practices
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Strict-Transport-Security"] =
                "max-age=31536000; includeSubDomains";

            // Enhanced Content-Security-Policy with development-friendly settings
            var csp =
                "default-src 'self'; "
                + $"script-src 'self' 'unsafe-inline' {allowedOrigin} https://js.stripe.com https://m.stripe.network https://gc.kis.v2.scr.kaspersky-labs.com https://stylecommerce-7o47.onrender.com https://stylecommerce.onrender.com; "
                + $"style-src 'self' 'unsafe-inline' {allowedOrigin} https://m.stripe.network https://js.stripe.com https://gc.kis.v2.scr.kaspersky-labs.com https://stylecommerce-7o47.onrender.com https://stylecommerce.onrender.com; "
                + "img-src 'self' data: https:; "
                + "font-src 'self' data:; "
                // Allow all localhost connections during development
                + $"connect-src 'self' {allowedOrigin} http://localhost:* ws://localhost:* https://api.stripe.com https://r.stripe.com https://stylecommerce-7o47.onrender.com https://stylecommerce.onrender.com https://merchant-ui-api.stripe.com https://*.stripe.com https://cdn.segment.com wss://gc.kis.v2.scr.kaspersky-labs.com; "
                + "frame-ancestors 'none'; "
                + "form-action 'self'; "
                + "base-uri 'self'; "
                + "object-src 'none'; "
                + "media-src 'self'; "
                + "frame-src https://js.stripe.com https://m.stripe.network; "
                + "child-src 'none'; "
                + "worker-src 'self';";

            context.Response.Headers["Content-Security-Policy"] = csp;

            // Log the CSP for debugging
            Console.WriteLine($"Setting CSP: {csp}");

            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Permissions-Policy"] =
                "geolocation=(), microphone=(), camera=()";
            context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";

            await _next(context);
        }
    }
}
