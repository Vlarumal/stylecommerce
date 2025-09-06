using Microsoft.EntityFrameworkCore;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly IAuditLoggingService _auditLoggingService;

        public UserService(
            ApplicationDbContext context,
            ILogger<UserService> logger,
            IAuditLoggingService auditLoggingService
        )
        {
            _context = context;
            _logger = logger;
            _auditLoggingService = auditLoggingService;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context
                .Users.Where(u => u.Id == id && u.DeletedAt == null)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context
                .Users.Where(u => u.Username == username && u.DeletedAt == null)
                .FirstOrDefaultAsync();
        }

        public async Task<User> CreateUserAsync(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.IsActive = true;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User created with ID: {UserId}", user.Id);

            await _auditLoggingService.LogActionAsync(
                "CREATE",
                "User",
                user.Id,
                user.Username,
                $"User {user.Username} created with email {user.Email}"
            );

            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User updated with ID: {UserId}", user.Id);

            await _auditLoggingService.LogActionAsync(
                "UPDATE",
                "User",
                user.Id,
                user.Username,
                $"User {user.Username} updated"
            );

            return user;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return false;
            }

            // For GDPR compliance, we implement "right to be forgotten"
            // by marking as deleted rather than hard deletion
            user.DeletedAt = DateTime.UtcNow;
            user.IsActive = false;
            user.Username = $"DELETED_{user.Id}";
            user.Email = $"deleted_{user.Id}@deleted.local";
            user.FirstName = "DELETED";
            user.LastName = "DELETED";
            user.PhoneNumber = null;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User marked as deleted with ID: {UserId}", id);

            await _auditLoggingService.LogActionAsync(
                "DELETE",
                "User",
                user.Id,
                user.Username,
                $"User {user.Username} marked as deleted (GDPR right to be forgotten)"
            );

            return true;
        }

        public async Task AnonymizeUserDataAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                // Anonymize user data for GDPR compliance
                user.Username = $"ANONYMIZED_{user.Id}";
                user.Email = $"anonymized_{user.Id}@anonymized.local";
                user.FirstName = "ANONYMIZED";
                user.LastName = "ANONYMIZED";
                user.PhoneNumber = null;
                user.UpdatedAt = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User data anonymized for ID: {UserId}", userId);

                await _auditLoggingService.LogActionAsync(
                    "ANONYMIZE",
                    "User",
                    user.Id,
                    user.Username,
                    $"User {user.Username} data anonymized for GDPR compliance"
                );
            }
        }

        public async Task<bool> UserExistsAsync(int id)
        {
            return await _context.Users.AnyAsync(u => u.Id == id && u.DeletedAt == null);
        }

        public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Users.FirstOrDefaultAsync(u =>
                u.RefreshToken == refreshToken && u.RefreshTokenExpiry > DateTime.UtcNow
            );
        }
    }
}
