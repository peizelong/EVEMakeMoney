using System.Security.Claims;
using EVEMakeMoney.Api.DTOs.Auth;
using EVEMakeMoney.Api.Services;
using EVEMakeMoney.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVEMakeMoney.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly EVESsoService _ssoService;

        public AuthController(AuthService authService, EVESsoService ssoService)
        {
            _authService = authService;
            _ssoService = ssoService;
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

        [Authorize]
        [HttpGet("sso/url")]
        public ActionResult<SsoUrlResponse> GetSsoUrl()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var url = _ssoService.GetAuthorizationUrl(userId);
            return Ok(new SsoUrlResponse { Url = url });
        }

        [HttpGet("/callback")]
        public async Task<IActionResult> SsoCallback(
            [FromQuery] string? code, 
            [FromQuery] string? state, 
            [FromQuery] string? error,
            [FromQuery] string? error_description)
        {
            var errorMessage = !string.IsNullOrEmpty(error_description) ? error_description : 
                              !string.IsNullOrEmpty(error) ? error : null;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                return Redirect($"{_ssoService.GetFrontendUrl()}?error={Uri.EscapeDataString(errorMessage)}");
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return Redirect($"{_ssoService.GetFrontendUrl()}?error=invalid_callback");
            }

            var (success, message, character) = await _ssoService.HandleCallbackAsync(code, state);

            if (!success)
            {
                return Redirect($"{_ssoService.GetFrontendUrl()}?error={Uri.EscapeDataString(message)}");
            }

            return Redirect($"{_ssoService.GetFrontendUrl()}?success=true&characterId={character?.CharacterId}&characterName={Uri.EscapeDataString(character?.CharacterName ?? "")}");
        }

        [Authorize]
        [HttpGet("characters")]
        public async Task<ActionResult<List<CharacterInfo>>> GetCharacters()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var characters = await _ssoService.GetUserCharactersAsync(userId);

            return Ok(characters.Select(c => new CharacterInfo
            {
                Id = c.Id,
                CharacterId = c.CharacterId,
                CharacterName = c.CharacterName,
                CorporationName = c.CorporationName,
                CorporationId = c.CorporationId,
                AllianceName = c.AllianceName,
                AllianceId = c.AllianceId,
                CreatedAt = c.CreatedAt,
                LastUsedAt = c.LastUsedAt
            }));
        }

        [Authorize]
        [HttpDelete("characters/{characterId}")]
        public async Task<ActionResult> UnbindCharacter(long characterId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var success = await _ssoService.UnbindCharacterAsync(userId, characterId);

            if (!success)
            {
                return NotFound(new { message = "Character not found" });
            }

            return Ok(new { message = "Character unbound successfully" });
        }
    }
}
