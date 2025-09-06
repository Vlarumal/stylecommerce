using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByRefreshTokenAsync(string refreshToken);
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int id);
        Task AnonymizeUserDataAsync(int userId);
        Task<bool> UserExistsAsync(int id);
    }
}
