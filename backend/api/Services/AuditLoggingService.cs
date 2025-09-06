using Microsoft.EntityFrameworkCore;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Services
{
    public class AuditLoggingService : IAuditLoggingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLoggingService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLoggingService(
            ApplicationDbContext context,
            ILogger<AuditLoggingService> logger,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogActionAsync(
            string actionType,
            string entityType,
            int? entityId = null,
            string? userId = null,
            string? additionalData = null
        )
        {
            try
            {
                var auditLog = new AuditLog
                {
                    ActionType = actionType,
                    EntityType = entityType,
                    EntityId = entityId,
                    UserId = userId ?? GetCurrentUserId(),
                    IpAddress = GetCurrentIpAddress(),
                    UserAgent = GetUserAgent(),
                    AdditionalData = additionalData,
                    CreatedAt = DateTime.UtcNow,
                    SessionId = GetSessionId(),
                };

                _context.AuditLogs.Add(auditLog);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save audit log: {Message}", ex.Message);
                    throw;
                }

                // Log to Serilog as well for real-time monitoring
                _logger.LogInformation(
                    "Audit Log - Action: {ActionType}, Entity: {EntityType}, EntityId: {EntityId}, User: {UserId}, IP: {IpAddress}",
                    actionType,
                    entityType,
                    entityId,
                    userId,
                    auditLog.IpAddress
                );
            }
            catch (Exception ex)
            {
                // Don't let audit logging failures break the main application flow
                _logger.LogError(ex, "Failed to log audit action: {ActionType}", actionType);
            }
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int page = 1, int pageSize = 50)
        {
            return await _context
                .AuditLogs.OrderByDescending(log => log.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(
            string userId,
            int page = 1,
            int pageSize = 50
        )
        {
            return await _context
                .AuditLogs.Where(log => log.UserId == userId)
                .OrderByDescending(log => log.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsByEntityTypeAsync(
            string entityType,
            int page = 1,
            int pageSize = 50
        )
        {
            return await _context
                .AuditLogs.Where(log => log.EntityType == entityType)
                .OrderByDescending(log => log.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetAuditLogCountAsync()
        {
            return await _context.AuditLogs.CountAsync();
        }

        public async Task CleanupOldLogsAsync(int daysToKeep = 365)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var oldLogs = await _context
                .AuditLogs.Where(log => log.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldLogs.Any())
            {
                _context.AuditLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Cleaned up {Count} audit logs older than {Days} days",
                    oldLogs.Count,
                    daysToKeep
                );
            }
        }

        private string? GetCurrentUserId()
        {
            // In a real implementation, this would extract the user ID from the JWT token
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.FindFirst("nameid")?.Value;
            }
            return null;
        }

        private string? GetCurrentIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            // Check for forwarded IP (from load balancers/proxies)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }

        private string? GetUserAgent()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Request.Headers["User-Agent"].FirstOrDefault();
        }

        private string? GetSessionId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            try
            {
                return httpContext.Session?.Id;
            }
            catch (InvalidOperationException)
            {
                // Session not configured, return null
                return null;
            }
        }
    }
}
