using System.Security.Claims;
using EVEMakeMoney.Api.Data;
using EVEMakeMoney.Api.DTOs.Assets;
using EVEMakeMoney.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EVEMakeMoney.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AssetsController : ControllerBase
    {
        private readonly EVEMakeMoneyDbContext _db;
        private readonly AssetService _assetService;
        private readonly EVESsoService _ssoService;

        public AssetsController(EVEMakeMoneyDbContext db, AssetService assetService, EVESsoService ssoService)
        {
            _db = db;
            _assetService = assetService;
            _ssoService = ssoService;
        }

        private async Task<CharacterEntity?> GetAndValidateCharacterAsync(long characterId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var character = await _db.Characters
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CharacterId == characterId);

            if (character == null)
                return null;

            if (string.IsNullOrEmpty(character.AccessToken))
                return null;

            if (character.TokenExpiresAt.HasValue && character.TokenExpiresAt.Value < DateTime.UtcNow)
            {
                var refreshResult = await _ssoService.RefreshCharacterTokenAsync(character);
                if (!refreshResult.Success)
                    return null;
            }

            return character;
        }

        [HttpGet("{characterId}")]
        public async Task<ActionResult<List<AssetWithLocation>>> GetAllAssets(long characterId)
        {
            var character = await GetAndValidateCharacterAsync(characterId);
            if (character == null)
            {
                return NotFound(new { message = "Character not found or not owned by user" });
            }

            try
            {
                var assets = await _assetService.GetAllAssetsWithLocationsAsync(character);
                return Ok(assets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch assets: " + ex.Message });
            }
        }

        [HttpGet("tree/{characterId}")]
        public async Task<ActionResult<List<AssetTreeNode>>> GetAssetsAsTree(long characterId)
        {
            var character = await GetAndValidateCharacterAsync(characterId);
            if (character == null)
            {
                return NotFound(new { message = "Character not found or not owned by user" });
            }

            try
            {
                var tree = await _assetService.GetAssetsAsTreeAsync(character);
                return Ok(tree);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch asset tree: " + ex.Message });
            }
        }

        [HttpGet("by-location/{characterId}")]
        public async Task<ActionResult<List<AssetGroupedByResolvedLocation>>> GetAssetsByLocation(long characterId)
        {
            var character = await GetAndValidateCharacterAsync(characterId);
            if (character == null)
            {
                return NotFound(new { message = "Character not found or not owned by user" });
            }

            try
            {
                var assets = await _assetService.GetAssetsGroupedByResolvedLocationAsync(character);
                return Ok(assets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch assets: " + ex.Message });
            }
        }

        [HttpGet("summary/{characterId}")]
        public async Task<ActionResult<AssetSummaryResponse>> GetAssetSummary(long characterId)
        {
            var character = await GetAndValidateCharacterAsync(characterId);
            if (character == null)
            {
                return NotFound(new { message = "Character not found or not owned by user" });
            }

            try
            {
                var summary = await _assetService.GetAssetSummaryAsync(character);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch asset summary: " + ex.Message });
            }
        }
    }
}
