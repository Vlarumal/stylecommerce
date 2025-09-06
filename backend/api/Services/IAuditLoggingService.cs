using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Services
{
    public interface IAuditLoggingService
    {
        Task LogActionAsync(
            string actionType,
            string entityType,
            int? entityId = null,
            string? userId = null,
            string? additionalData = null
        );
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int page = 1, int pageSize = 50);
        Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(
            string userId,
            int page = 1,
            int pageSize = 50
        );
        Task<IEnumerable<AuditLog>> GetAuditLogsByEntityTypeAsync(
            string entityType,
            int page = 1,
            int pageSize = 50
        );
        Task<int> GetAuditLogCountAsync();
        Task CleanupOldLogsAsync(int daysToKeep = 365);
    }
}
