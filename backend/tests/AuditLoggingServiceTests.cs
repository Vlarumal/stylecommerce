using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Models;
using StyleCommerce.Api.Services;
using Xunit;

namespace StyleCommerce.Api.Tests
{
    public class AuditLoggingServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLoggingService _auditLoggingService;
        private readonly Mock<ILogger<AuditLoggingService>> _loggerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

        public AuditLoggingServiceTests()
        {
            // Set up in-memory database with a unique name for this test instance
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDatabase_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<AuditLoggingService>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _auditLoggingService = new AuditLoggingService(
                _context,
                _loggerMock.Object,
                _httpContextAccessorMock.Object
            );

            _context.AuditLogs.AddRange(
                new AuditLog
                {
                    ActionType = "CREATE",
                    EntityType = "User",
                    EntityId = 1,
                    UserId = "user1",
                    IpAddress = "192.168.1.100",
                    UserAgent = "TestAgent",
                    AdditionalData = "Test data",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                },
                new AuditLog
                {
                    ActionType = "UPDATE",
                    EntityType = "User",
                    EntityId = 2,
                    UserId = "user2",
                    IpAddress = "192.168.1.2",
                    UserAgent = "TestAgent2",
                    AdditionalData = "Test data 2",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                },
                new AuditLog
                {
                    ActionType = "DELETE",
                    EntityType = "Product",
                    EntityId = 1,
                    UserId = "user1",
                    IpAddress = "192.168.1.1",
                    UserAgent = "TestAgent",
                    AdditionalData = "Test data 3",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-2),
                }
            );
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task LogActionAsync_ValidData_CreatesAuditLog()
        {
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(default(HttpContext));

            await _auditLoggingService.LogActionAsync(
                "TEST_ACTION",
                "TestEntity",
                123,
                "testuser",
                "Test additional data"
            );

            var auditLogs = await _context.AuditLogs.ToListAsync();
            Assert.Equal(4, auditLogs.Count);

            var newLog = auditLogs.Last();
            Assert.Equal("TEST_ACTION", newLog.ActionType);
            Assert.Equal("TestEntity", newLog.EntityType);
            Assert.Equal(123, newLog.EntityId);
            Assert.Equal("testuser", newLog.UserId);
            Assert.Equal("Test additional data", newLog.AdditionalData);
        }

        [Fact]
        public async Task LogActionAsync_WithHttpContext_ExtractsContextData()
        {
            // Create a separate test with its own context to avoid conflicts with seeded data
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase_Isolated")
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                var loggerMock = new Mock<ILogger<AuditLoggingService>>();
                var httpContextAccessorMock = new Mock<IHttpContextAccessor>();

                var service = new AuditLoggingService(
                    context,
                    loggerMock.Object,
                    httpContextAccessorMock.Object
                );

                var httpContext = new DefaultHttpContext();
                httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(
                    "192.168.1.100"
                );
                httpContext.Request.Headers["User-Agent"] = "TestBrowser";

                // Set up a fake authenticated user
                var claims = new List<Claim> { new Claim("sub", "testuser") };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                httpContext.User = new ClaimsPrincipal(identity);

                httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

                await service.LogActionAsync("TEST_ACTION", "TestEntity");

                // Verify the log was created
                var newLog = await context
                    .AuditLogs.AsNoTracking()
                    .Where(l => l.ActionType == "TEST_ACTION" && l.EntityType == "TestEntity")
                    .OrderByDescending(l => l.CreatedAt)
                    .FirstOrDefaultAsync();

                Assert.NotNull(newLog);
                Assert.Equal("192.168.1.100", newLog.IpAddress);
                Assert.Equal("TestBrowser", newLog.UserAgent);
                Assert.Equal("testuser", newLog.UserId);
            }
        }

        [Fact]
        public async Task GetAuditLogsAsync_ReturnsPagedResults()
        {
            var result = await _auditLoggingService.GetAuditLogsAsync(1, 2);

            var logs = result.ToList();
            Assert.Equal(2, logs.Count);
            // Most recent first (ordered by CreatedAt descending)
            Assert.Equal("DELETE", logs[0].ActionType);
            Assert.Equal("UPDATE", logs[1].ActionType);
        }

        [Fact]
        public async Task GetAuditLogsByUserAsync_ReturnsUserLogs()
        {
            var result = await _auditLoggingService.GetAuditLogsByUserAsync("user1", 1, 10);

            var logs = result.ToList();
            Assert.Equal(2, logs.Count);
            Assert.All(logs, log => Assert.Equal("user1", log.UserId));
        }

        [Fact]
        public async Task GetAuditLogsByEntityTypeAsync_ReturnsEntityTypeLogs()
        {
            var result = await _auditLoggingService.GetAuditLogsByEntityTypeAsync("User", 1, 10);

            var logs = result.ToList();
            Assert.Equal(2, logs.Count);
            Assert.All(logs, log => Assert.Equal("User", log.EntityType));
        }

        [Fact]
        public async Task GetAuditLogCountAsync_ReturnsCorrectCount()
        {
            var result = await _auditLoggingService.GetAuditLogCountAsync();

            Assert.Equal(3, result);
        }

        [Fact]
        public async Task CleanupOldLogsAsync_OldLogs_RemovesExpiredLogs()
        {
            var oldLog = new AuditLog
            {
                ActionType = "OLD_ACTION",
                EntityType = "OldEntity",
                CreatedAt = DateTime.UtcNow.AddDays(-400),
            };
            _context.AuditLogs.Add(oldLog);
            _context.SaveChanges();

            await _auditLoggingService.CleanupOldLogsAsync(365);

            var remainingLogs = await _context.AuditLogs.ToListAsync();
            Assert.Equal(3, remainingLogs.Count);
            Assert.DoesNotContain(remainingLogs, log => log.ActionType == "OLD_ACTION");
        }

        [Fact]
        public async Task CleanupOldLogsAsync_NoOldLogs_NoChanges()
        {
            var recentLog = new AuditLog
            {
                ActionType = "RECENT_ACTION",
                EntityType = "RecentEntity",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
            };
            _context.AuditLogs.Add(recentLog);
            _context.SaveChanges();

            var initialCount = await _context.AuditLogs.CountAsync();

            await _auditLoggingService.CleanupOldLogsAsync(365);

            var finalCount = await _context.AuditLogs.CountAsync();
            Assert.Equal(initialCount, finalCount);
        }
    }
}
