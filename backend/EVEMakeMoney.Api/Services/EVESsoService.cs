using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EVEMakeMoney.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EVEMakeMoney.Api.Services
{
    public class EVESsoService
    {
        private readonly EVEMakeMoneyDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _callbackUrl;
        private readonly string _ssoBaseUrl;
        private readonly string _ssoMetaDataUrl;

        private const string SSO_TOKEN_URL = "/oauth/token";
        private const string SSO_META_DATA_URL = "https://login.eveonline.com/.well-known/oauth-authorization-server";

        public EVESsoService(EVEMakeMoneyDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
            _httpClient = new HttpClient();

            _clientId = _configuration["EveSso:ClientId"] ?? "";
            _clientSecret = _configuration["EveSso:ClientSecret"] ?? "";
            _callbackUrl = _configuration["EveSso:CallbackUrl"] ?? "http://localhost:5000/api/auth/sso/callback";

            var dataSource = _configuration["EveSso:DataSource"]?.ToLower() ?? "tranquility";
            _ssoBaseUrl = dataSource == "serenity"
                ? "https://login.evepc.163.com/v2"
                : "https://login.eveonline.com/v2";
            _ssoMetaDataUrl = dataSource == "serenity"
                ? "https://login.evepc.163.com/.well-known/oauth-authorization-server"
                : SSO_META_DATA_URL;
        }

        public string GetAuthorizationUrl(int userId)
        {
            var state = GenerateState(userId);
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            StoreCodeVerifier(state, codeVerifier);

            var scopes = new[]
            {
                "publicData",
                "esi-calendar.respond_calendar_events.v1",
                "esi-calendar.read_calendar_events.v1",
                "esi-location.read_location.v1",
                "esi-location.read_ship_type.v1",
                "esi-mail.organize_mail.v1",
                "esi-mail.read_mail.v1",
                "esi-mail.send_mail.v1",
                "esi-skills.read_skills.v1",
                "esi-skills.read_skillqueue.v1",
                "esi-wallet.read_character_wallet.v1",
                "esi-wallet.read_corporation_wallet.v1",
                "esi-search.search_structures.v1",
                "esi-clones.read_clones.v1",
                "esi-characters.read_contacts.v1",
                "esi-universe.read_structures.v1",
                "esi-killmails.read_killmails.v1",
                "esi-corporations.read_corporation_membership.v1",
                "esi-assets.read_assets.v1",
                "esi-planets.manage_planets.v1",
                "esi-fleets.read_fleet.v1",
                "esi-fleets.write_fleet.v1",
                "esi-ui.open_window.v1",
                "esi-ui.write_waypoint.v1",
                "esi-characters.write_contacts.v1",
                "esi-fittings.read_fittings.v1",
                "esi-fittings.write_fittings.v1",
                "esi-markets.structure_markets.v1",
                "esi-corporations.read_structures.v1",
                "esi-characters.read_loyalty.v1",
                "esi-characters.read_chat_channels.v1",
                "esi-characters.read_medals.v1",
                "esi-characters.read_standings.v1",
                "esi-characters.read_agents_research.v1",
                "esi-industry.read_character_jobs.v1",
                "esi-markets.read_character_orders.v1",
                "esi-characters.read_blueprints.v1",
                "esi-characters.read_corporation_roles.v1",
                "esi-location.read_online.v1",
                "esi-contracts.read_character_contracts.v1",
                "esi-clones.read_implants.v1",
                "esi-characters.read_fatigue.v1",
                "esi-killmails.read_corporation_killmails.v1",
                "esi-corporations.track_members.v1",
                "esi-wallet.read_corporation_wallets.v1",
                "esi-characters.read_notifications.v1",
                "esi-corporations.read_divisions.v1",
                "esi-corporations.read_contacts.v1",
                "esi-assets.read_corporation_assets.v1",
                "esi-corporations.read_titles.v1",
                "esi-corporations.read_blueprints.v1",
                "esi-contracts.read_corporation_contracts.v1",
                "esi-corporations.read_standings.v1",
                "esi-corporations.read_starbases.v1",
                "esi-industry.read_corporation_jobs.v1",
                "esi-markets.read_corporation_orders.v1",
                "esi-corporations.read_container_logs.v1",
                "esi-industry.read_character_mining.v1",
                "esi-industry.read_corporation_mining.v1",
                "esi-planets.read_customs_offices.v1",
                "esi-corporations.read_facilities.v1",
                "esi-corporations.read_medals.v1",
                "esi-characters.read_titles.v1",
                "esi-alliances.read_contacts.v1",
                "esi-characters.read_fw_stats.v1",
                "esi-corporations.read_fw_stats.v1",
                "esi-corporations.read_projects.v1",
                "esi-corporations.read_freelance_jobs.v1"
            };
            var scopeString = string.Join(" ", scopes);

            var url = $"{_ssoBaseUrl}/oauth/authorize/?response_type=code&redirect_uri={Uri.EscapeDataString(_callbackUrl)}&client_id={_clientId}&state={state}&code_challenge={codeChallenge}&code_challenge_method=S256&scope={Uri.EscapeDataString(scopeString)}";

            return url;
        }

        public string GetFrontendUrl()
        {
            var baseUrl = _configuration["EveSso:FrontendUrl"] ?? "http://localhost:5173";
            return baseUrl;
        }

        public async Task<(bool Success, string Message, CharacterEntity? Character)> HandleCallbackAsync(string code, string state)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return (false, "Invalid callback parameters", null);
            }

            var (valid, userId) = ValidateState(state);
            if (!valid || userId == null)
            {
                return (false, "Invalid state parameter", null);
            }

            var codeVerifier = GetCodeVerifier(state);
            if (string.IsNullOrEmpty(codeVerifier))
            {
                return (false, "Code verifier not found", null);
            }

            var tokenDetails = await ExchangeCodeForTokenAsync(code, codeVerifier);
            if (tokenDetails == null)
            {
                return (false, "Failed to exchange code for token", null);
            }

            var characterDetails = await GetCharacterDetailsAsync(tokenDetails.AccessToken);
            if (characterDetails == null)
            {
                return (false, "Failed to get character details", null);
            }

            var existingCharacter = await _db.Characters
                .FirstOrDefaultAsync(c => c.CharacterId == characterDetails.CharacterId);

            if (existingCharacter != null)
            {
                if (existingCharacter.UserId != userId.Value)
                {
                    return (false, "This EVE character is already bound to another account", null);
                }

                existingCharacter.AccessToken = tokenDetails.AccessToken;
                existingCharacter.RefreshToken = tokenDetails.RefreshToken;
                existingCharacter.TokenExpiresAt = tokenDetails.ExpiresUtc;
                existingCharacter.Scopes = tokenDetails.Scopes;
                existingCharacter.LastUsedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return (true, "Character token updated", existingCharacter);
            }

            var character = new CharacterEntity
            {
                UserId = userId.Value,
                CharacterId = characterDetails.CharacterId,
                CharacterName = characterDetails.CharacterName,
                CharacterOwnerHash = characterDetails.CharacterOwnerHash,
                AccessToken = tokenDetails.AccessToken,
                RefreshToken = tokenDetails.RefreshToken,
                TokenExpiresAt = tokenDetails.ExpiresUtc,
                Scopes = tokenDetails.Scopes,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow
            };

            _db.Characters.Add(character);
            await _db.SaveChangesAsync();

            return (true, "Character bound successfully", character);
        }

        public async Task<List<CharacterEntity>> GetUserCharactersAsync(int userId)
        {
            return await _db.Characters
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.LastUsedAt)
                .ToListAsync();
        }

        public async Task<bool> UnbindCharacterAsync(int userId, long characterId)
        {
            var character = await _db.Characters
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CharacterId == characterId);

            if (character == null)
            {
                return false;
            }

            _db.Characters.Remove(character);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> RefreshCharacterTokenAsync(CharacterEntity character)
        {
            if (string.IsNullOrEmpty(character.RefreshToken))
            {
                return (false, "No refresh token available");
            }

            try
            {
                var byteArray = Encoding.ASCII.GetBytes(_clientId + ":" + _clientSecret);
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", character.RefreshToken),
                    new KeyValuePair<string, string>("client_id", _clientId)
                });

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(_ssoBaseUrl + SSO_TOKEN_URL),
                    Method = HttpMethod.Post,
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return (false, "Failed to refresh token");
                }

                var tokenDetails = JsonSerializer.Deserialize<AccessTokenDetails>(await response.Content.ReadAsStringAsync());
                if (tokenDetails == null)
                {
                    return (false, "Failed to parse token response");
                }

                character.AccessToken = tokenDetails.AccessToken;
                character.RefreshToken = tokenDetails.RefreshToken;
                character.TokenExpiresAt = tokenDetails.ExpiresUtc;
                character.Scopes = tokenDetails.Scopes;
                character.LastUsedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return (true, "Token refreshed successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error refreshing token: {ex.Message}");
            }
        }

        private async Task<AccessTokenDetails?> ExchangeCodeForTokenAsync(string code, string codeVerifier)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("code_verifier", codeVerifier)
                });

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(_ssoBaseUrl + SSO_TOKEN_URL),
                    Method = HttpMethod.Post,
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return JsonSerializer.Deserialize<AccessTokenDetails>(await response.Content.ReadAsStringAsync());
            }
            catch
            {
                return null;
            }
        }

        private async Task<CharacterDetails?> GetCharacterDetailsAsync(string accessToken)
        {
            try
            {
                var parts = accessToken.Split('.');
                if (parts.Length != 3)
                {
                    return null;
                }

                var payload = parts[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                while (payload.Length % 4 != 0)
                {
                    payload += '=';
                }

                var payloadBytes = Convert.FromBase64String(payload);
                var payloadJson = Encoding.UTF8.GetString(payloadBytes);

                using var doc = JsonDocument.Parse(payloadJson);
                var root = doc.RootElement;

                var sub = root.GetProperty("sub").GetString();
                var name = root.GetProperty("name").GetString();
                var owner = root.GetProperty("owner").GetString();
                var azp = root.GetProperty("azp").GetString();
                var exp = root.GetProperty("exp").GetInt64();

                var characterIdStr = sub?.Split(':')[2];
                if (!long.TryParse(characterIdStr, out var characterId))
                {
                    return null;
                }

                return new CharacterDetails
                {
                    CharacterId = characterId,
                    CharacterName = name ?? "",
                    CharacterOwnerHash = owner ?? "",
                    ClientId = azp ?? "",
                    ExpiresOn = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime,
                    TokenType = "JWT"
                };
            }
            catch
            {
                return null;
            }
        }

        private static string GenerateState(int userId)
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var state = Convert.ToBase64String(randomBytes).TrimEnd('=');
            return $"{userId}:{state}";
        }

        private static (bool Valid, int? UserId) ValidateState(string state)
        {
            try
            {
                var parts = state.Split(':');
                if (parts.Length < 2)
                {
                    return (false, null);
                }

                if (!int.TryParse(parts[0], out var userId))
                {
                    return (false, null);
                }

                return (true, userId);
            }
            catch
            {
                return (false, null);
            }
        }

        private static string GenerateCodeVerifier()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).TrimEnd('=');
        }

        private static string GenerateCodeChallenge(string codeVerifier)
        {
            var bytes = Encoding.ASCII.GetBytes(codeVerifier);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static readonly Dictionary<string, string> _codeVerifiers = new();
        private static readonly object _lock = new();

        private static void StoreCodeVerifier(string state, string codeVerifier)
        {
            lock (_lock)
            {
                _codeVerifiers[state] = codeVerifier;
            }
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(10));
                lock (_lock)
                {
                    _codeVerifiers.Remove(state);
                }
            });
        }

        private static string? GetCodeVerifier(string state)
        {
            lock (_lock)
            {
                return _codeVerifiers.TryGetValue(state, out var verifier) ? verifier : null;
            }
        }
    }

    public class AccessTokenDetails
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn
        {
            get => _expiresIn;
            set
            {
                ExpiresUtc = DateTime.UtcNow.AddSeconds(value);
                _expiresIn = value;
            }
        }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string? Scopes { get; set; }

        public DateTime ExpiresUtc { get; set; }

        private int _expiresIn;
    }

    public class CharacterDetails
    {
        public long CharacterId { get; set; }
        public string CharacterName { get; set; } = string.Empty;
        public string CharacterOwnerHash { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public DateTime ExpiresOn { get; set; }
        public string TokenType { get; set; } = string.Empty;
    }
}
