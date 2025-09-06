using System.Security.Claims;

namespace StyleCommerce.Api.Services
{
    public interface IJwtService
    {
        string GenerateToken(string username, IEnumerable<Claim>? additionalClaims = null);
        ClaimsPrincipal ValidateToken(string token);
    }
}
