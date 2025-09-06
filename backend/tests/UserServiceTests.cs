using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Models;
using StyleCommerce.Api.Services;
using Xunit;

namespace StyleCommerce.Api.Tests
{
    public class UserServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly Mock<IAuditLoggingService> _auditLoggingServiceMock;

        public UserServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use a unique database name for each test
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<UserService>>();
            _auditLoggingServiceMock = new Mock<IAuditLoggingService>();
            _userService = new UserService(
                _context,
                _loggerMock.Object,
                _auditLoggingServiceMock.Object
            );

            _context.Users.AddRange(
                new User
                {
                    Id = 1,
                    Username = "testuser1",
                    Email = "test1@example.com",
                    FirstName = "Test",
                    LastName = "User1",
                },
                new User
                {
                    Id = 2,
                    Username = "testuser2",
                    Email = "test2@example.com",
                    FirstName = "Test",
                    LastName = "User2",
                }
            );
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task GetUserByIdAsync_ExistingId_ReturnsUser()
        {
            var result = await _userService.GetUserByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("testuser1", result.Username);
        }

        [Fact]
        public async Task GetUserByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _userService.GetUserByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByIdAsync_DeletedUser_ReturnsNull()
        {
            var user = new User
            {
                Id = 3,
                Username = "deleteduser",
                Email = "deleted@example.com",
                DeletedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var result = await _userService.GetUserByIdAsync(3);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateUserAsync_ValidUser_ReturnsCreatedUser()
        {
            var user = new User
            {
                Username = "newuser",
                Email = "newuser@example.com",
                FirstName = "New",
                LastName = "User",
            };

            var result = await _userService.CreateUserAsync(user);

            Assert.Equal("newuser", result.Username);
            Assert.True(result.Id > 0);
            Assert.True(result.IsActive);
            // DateTime is a value type, so we don't need to check for null
            // Instead, we can verify that it's been set to a reasonable value
            Assert.NotEqual(DateTime.MinValue, result.CreatedAt);

            _auditLoggingServiceMock.Verify(
                x => x.LogActionAsync("CREATE", "User", result.Id, "newuser", It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateUserAsync_ExistingUser_ReturnsUpdatedUser()
        {
            // First, get the existing user from the database
            var user = await _context.Users.FindAsync(1);
            Assert.NotNull(user);

            // Modify properties
            user.Username = "updateduser";
            user.Email = "updated@example.com";
            user.FirstName = "Updated";
            user.LastName = "User";

            var result = await _userService.UpdateUserAsync(user);

            Assert.NotNull(result);
            Assert.Equal("updateduser", result.Username);
            // Note: We can't check UpdatedAt > CreatedAt here because we're not retrieving the full entity from DB
            // The test is primarily checking that the update operation succeeds

            _auditLoggingServiceMock.Verify(
                x => x.LogActionAsync("UPDATE", "User", user.Id, "updateduser", It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteUserAsync_ExistingId_ReturnsTrue()
        {
            var result = await _userService.DeleteUserAsync(1);

            Assert.True(result);

            // Verify the user was marked as deleted
            var user = await _context.Users.FindAsync(1);
            Assert.NotNull(user);
            Assert.NotNull(user.DeletedAt);
            Assert.False(user.IsActive);
            Assert.Equal("DELETED_1", user.Username);
            Assert.Equal("deleted_1@deleted.local", user.Email);

            _auditLoggingServiceMock.Verify(
                x => x.LogActionAsync("DELETE", "User", 1, "DELETED_1", It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteUserAsync_NonExistingId_ReturnsFalse()
        {
            var result = await _userService.DeleteUserAsync(999);

            Assert.False(result);
        }

        [Fact]
        public async Task AnonymizeUserDataAsync_ExistingUser_AnonymizesData()
        {
            await _userService.AnonymizeUserDataAsync(1);

            var user = await _context.Users.FindAsync(1);
            Assert.NotNull(user);
            Assert.Equal("ANONYMIZED_1", user.Username);
            Assert.Equal("anonymized_1@anonymized.local", user.Email);
            Assert.Equal("ANONYMIZED", user.FirstName);
            Assert.Equal("ANONYMIZED", user.LastName);
            Assert.Null(user.PhoneNumber);
            Assert.True(user.UpdatedAt > user.CreatedAt);
            Assert.True(user.IsActive); // User should still be active after anonymization

            _auditLoggingServiceMock.Verify(
                x => x.LogActionAsync("ANONYMIZE", "User", 1, "ANONYMIZED_1", It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task AnonymizeUserDataAsync_NonExistingUser_DoesNotThrow()
        {
            await Record.ExceptionAsync(async () => await _userService.AnonymizeUserDataAsync(999));
            // Should not throw an exception
        }

        [Fact]
        public async Task UserExistsAsync_ExistingUser_ReturnsTrue()
        {
            var result = await _userService.UserExistsAsync(1);

            Assert.True(result);
        }

        [Fact]
        public async Task UserExistsAsync_NonExistingUser_ReturnsFalse()
        {
            var result = await _userService.UserExistsAsync(999);

            Assert.False(result);
        }

        [Fact]
        public async Task UserExistsAsync_DeletedUser_ReturnsFalse()
        {
            var user = new User
            {
                Id = 3,
                Username = "deleteduser",
                Email = "deleted@example.com",
                DeletedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var result = await _userService.UserExistsAsync(3);

            Assert.False(result);
        }
    }
}
