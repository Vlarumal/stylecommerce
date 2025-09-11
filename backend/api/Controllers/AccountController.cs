using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using StyleCommerce.Api.Models;
using StyleCommerce.Api.Services;
using StyleCommerce.Api.Utils;

namespace StyleCommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IJwtService _jwtService;
        private readonly IUserService _userService;

        public AccountController(IJwtService jwtService, IUserService userService)
        {
            _jwtService = jwtService;
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest("Username and password are required");
            }

            var user = await _userService.GetUserByUsernameAsync(model.Username);
            if (user == null)
            {
                return Unauthorized("Invalid username or password");
            }

            var passwordHasher = new PasswordHasher();
            if (
                string.IsNullOrEmpty(user.PasswordHash)
                || !passwordHasher.VerifyPassword(model.Password, user.PasswordHash)
            )
            {
                return Unauthorized("Invalid username or password");
            }

            var additionalClaims = new List<Claim>
            {
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role), // Add role claim
            };
            var token = _jwtService.GenerateToken(model.Username, additionalClaims);

            var refreshToken = Guid.NewGuid().ToString();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _userService.UpdateUserAsync(user);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(30),
            };

            Response.Cookies.Append("AuthToken", token, cookieOptions);

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7),
            };

            Response.Cookies.Append("RefreshToken", refreshToken, refreshCookieOptions);

            return Ok(new { Success = true });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (
                string.IsNullOrEmpty(model.Username)
                || string.IsNullOrEmpty(model.Password)
                || string.IsNullOrEmpty(model.Email)
            )
            {
                return BadRequest("Username, password, and email are required");
            }

            var existingUser = await _userService.GetUserByUsernameAsync(model.Username);
            if (existingUser != null)
            {
                return BadRequest("Username already exists");
            }

            var passwordHasher = new PasswordHasher();
            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                FirstName = model.FirstName ?? model.Username,
                LastName = model.LastName ?? "User",
                PasswordHash = passwordHasher.HashPassword(model.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            user = await _userService.CreateUserAsync(user);

            return Ok(new { Success = true, Message = "User registered successfully" });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["RefreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest("Refresh token is required");
            }

            var user = await _userService.GetUserByRefreshTokenAsync(refreshToken);
            if (user == null)
            {
                return BadRequest("Invalid refresh token");
            }

            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            {
                return BadRequest("Refresh token has expired");
            }

            try
            {
                var additionalClaims = new List<Claim>
                {
                    new Claim("UserId", user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role),
                };
                var token = _jwtService.GenerateToken(user.Username, additionalClaims);

                var newRefreshToken = Guid.NewGuid().ToString();
                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                await _userService.UpdateUserAsync(user);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddMinutes(30),
                };

                Response.Cookies.Append("AuthToken", token, cookieOptions);

                var refreshCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(7),
                };

                Response.Cookies.Append("RefreshToken", newRefreshToken, refreshCookieOptions);

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("check-auth")]
        public IActionResult CheckAuth()
        {
            if (Request.Cookies["AuthToken"] != null)
            {
                var token = Request.Cookies["AuthToken"];
                var principal = _jwtService.ValidateToken(token);

                if (principal != null)
                {
                    var username = principal.FindFirst(ClaimTypes.Name)?.Value;
                    var userId = principal.FindFirst("UserId")?.Value;
                    var role = principal.FindFirst(ClaimTypes.Role)?.Value ?? "User";

                    return Ok(
                        new
                        {
                            IsAuthenticated = true,
                            Username = username,
                            UserId = userId,
                            Role = role,
                        }
                    );
                }
            }

            return Ok(new { IsAuthenticated = false });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken");

            Response.Cookies.Delete("RefreshToken");

            return Ok(new { Success = true });
        }
    }

    public class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
