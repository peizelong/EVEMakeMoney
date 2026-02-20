using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EVEMakeMoney.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EVEMakeMoney.Api.Services
{
    public class AuthService
    {
        private readonly EVEMakeMoneyDbContext _db;
        private readonly IConfiguration _configuration;

        public AuthService(EVEMakeMoneyDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public async Task<(bool Success, string Message, UserEntity? User)> RegisterAsync(string username, string email, string password)
        {
            if (await _db.Users.AnyAsync(u => u.Username == username))
            {
                return (false, "用户名已存在", null);
            }

            if (await _db.Users.AnyAsync(u => u.Email == email))
            {
                return (false, "邮箱已被注册", null);
            }

            var user = new UserEntity
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return (true, "注册成功", user);
        }

        public async Task<(bool Success, string Message, AuthResponse? Response)> LoginAsync(string username, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return (false, "用户名或密码错误", null);
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();
            
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _db.SaveChangesAsync();

            return (true, "登录成功", new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
                User = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email
                }
            });
        }

        public async Task<(bool Success, AuthResponse? Response)> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            var principal = GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
            {
                return (false, null);
            }

            var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _db.Users.FindAsync(userId);

            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return (false, null);
            }

            var newAccessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _db.SaveChangesAsync();

            return (true, new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
                User = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email
                }
            });
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return false;

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _db.SaveChangesAsync();
            return true;
        }

        private string GenerateAccessToken(UserEntity user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "EVEMakeMoney_SecretKey_ForDevelopment_2024";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "EVEMakeMoney",
                audience: _configuration["Jwt:Audience"] ?? "EVEMakeMoney",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "EVEMakeMoney_SecretKey_ForDevelopment_2024";
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                
                if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private int GetTokenExpirationHours()
        {
            return _configuration.GetValue<int>("Jwt:ExpirationHours", 24);
        }
    }

    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserInfo User { get; set; } = new();
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
