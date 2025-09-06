using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleCommerce.Api.Services;

namespace StyleCommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditLoggingService _auditLoggingService;

        public AuditController(IAuditLoggingService auditLoggingService)
        {
            _auditLoggingService = auditLoggingService;
        }

        [HttpGet]
        public async Task<ActionResult> GetAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50
        )
        {
            if (page < 1)
                page = 1;
            if (pageSize < 1 || pageSize > 100)
                pageSize = 50;

            var auditLogs = await _auditLoggingService.GetAuditLogsAsync(page, pageSize);
            var totalCount = await _auditLoggingService.GetAuditLogCountAsync();

            var result = new
            {
                Data = auditLogs,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            };

            return Ok(result);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetAuditLogsByUser(
            string userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50
        )
        {
            if (page < 1)
                page = 1;
            if (pageSize < 1 || pageSize > 100)
                pageSize = 50;

            var auditLogs = await _auditLoggingService.GetAuditLogsByUserAsync(
                userId,
                page,
                pageSize
            );
            var totalCount = auditLogs.Count();

            var result = new
            {
                Data = auditLogs,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            };

            return Ok(result);
        }

        [HttpGet("entity/{entityType}")]
        public async Task<ActionResult> GetAuditLogsByEntityType(
            string entityType,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50
        )
        {
            if (page < 1)
                page = 1;
            if (pageSize < 1 || pageSize > 100)
                pageSize = 50;

            var auditLogs = await _auditLoggingService.GetAuditLogsByEntityTypeAsync(
                entityType,
                page,
                pageSize
            );
            var totalCount = auditLogs.Count();

            var result = new
            {
                Data = auditLogs,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            };

            return Ok(result);
        }

        [HttpPost("cleanup")]
        public async Task<ActionResult> CleanupOldLogs([FromQuery] int daysToKeep = 365)
        {
            if (daysToKeep < 1)
                daysToKeep = 365;
            if (daysToKeep > 3650)
                daysToKeep = 3650;

            await _auditLoggingService.CleanupOldLogsAsync(daysToKeep);
            return Ok(
                new { Message = $"Audit logs older than {daysToKeep} days have been cleaned up" }
            );
        }
    }
}
