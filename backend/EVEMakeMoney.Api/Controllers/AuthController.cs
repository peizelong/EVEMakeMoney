using System.Security.Claims;
using EVEMakeMoney.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVEMakeMoney.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var (success, message, user) = await _authService.RegisterAsync(request.Username, request.Email, request.Password);
            
            if (!success)
            {
                return BadRequest(new { message });
            }

            var loginResult = await _authService.LoginAsync(request.Username, request.Password);
            return Ok(loginResult.Response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var (success, message, response) = await _authService.LoginAsync(request.Username, request.Password);
            
            if (!success)
            {
                return Unauthorized(new { message });
            }

            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var (success, response) = await _authService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);
            
            if (!success)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            return Ok(response);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _authService.LogoutAsync(userId);
            return Ok(new { message = "Logged out successfully" });
        }

        [Authorize]
        [HttpGet("me")]
        public ActionResult<UserInfo> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            return Ok(new UserInfo
            {
                Id = int.Parse(userId ?? "0"),
                Username = username ?? "",
                Email = email ?? ""
            });
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
