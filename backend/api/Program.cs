using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Serilog;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Middleware;
using StyleCommerce.Api.Models;
using StyleCommerce.Api.Services;
using StyleCommerce.Api.Validators;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/stylecommerce-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPaymentTokenizationService, PaymentTokenizationService>();
builder.Services.AddScoped<IAuditLoggingService, AuditLoggingService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<PaymentProcessor>();
builder.Services.AddScoped<IOrderService, OrderService>(provider => new OrderService(
    provider.GetRequiredService<ApplicationDbContext>(),
    provider.GetRequiredService<ICartService>(),
    provider.GetRequiredService<IProductService>(),
    provider.GetRequiredService<PaymentProcessor>(),
    provider.GetRequiredService<ILogger<OrderService>>()
));
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection("SecuritySettings"));
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("StripeSettings"));

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddValidatorsFromAssemblyContaining<ProductValidator>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowSpecificOrigins",
        policy =>
        {
            policy
                .WithOrigins(
                    builder
                        .Configuration.GetSection("SecuritySettings:AllowedCorsOrigins")
                        .Get<string[]>()
                        ?? new string[]
                        {
                            "https://stylecommerce-7o47.onrender.com",
                            "https://stylecommerce.onrender.com",
                            "http://localhost:5173",
                            "https://localhost:5173",
                            "https://localhost:4173",
                            "http://localhost:4173",
                        }
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    );
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(
        "CartPolicy",
        context =>
        {
            var userId =
                context.User.Identity?.IsAuthenticated == true
                    ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    : context.Request.Cookies["CartSessionId"];

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: userId
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous",
                partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 10,
                    Window = TimeSpan.FromSeconds(10),
                }
            );
        }
    );
});

var stripeSettings = builder.Configuration.GetSection("StripeSettings").Get<StripeSettings>();
if (!string.IsNullOrEmpty(stripeSettings?.SecretKey))
{
    Stripe.StripeConfiguration.ApiKey = stripeSettings.SecretKey;
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "images");

if (!Directory.Exists(imagesPath))
{
    var workspaceRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../.."));
    var devImagesPath = Path.Combine(workspaceRoot, "images");
    if (Directory.Exists(devImagesPath))
    {
        imagesPath = devImagesPath;
    }
    else
    {
        Directory.CreateDirectory(imagesPath);
    }
}

Console.WriteLine($"Serving static files from: {imagesPath}");

app.UseStaticFiles(
    new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(imagesPath),
        RequestPath = "/images",
        ServeUnknownFileTypes = true, // Allow serving .png files
        DefaultContentType = "image/png",
    }
);

app.UseCors("AllowSpecificOrigins");
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var retries = 10;
    var delay = 2000; // milliseconds

    for (int i = 0; i < retries; i++)
    {
        try
        {
            logger.LogInformation(
                "Attempting to apply database migrations (attempt {Attempt}/{Total})",
                i + 1,
                retries
            );
            context.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully");
            break;
        }
        catch (NpgsqlException ex)
        {
            logger.LogError(ex, "Migration attempt {Attempt}/{Total} failed", i + 1, retries);
            if (i == retries - 1)
                throw;

            logger.LogInformation("Waiting {Delay}ms before next migration attempt", delay);
            Thread.Sleep(delay);
            delay *= 2;
        }
    }
}

app.Run();
