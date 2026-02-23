using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EVEMakeMoney.Api.Data;

namespace EVEMakeMoney.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BlueprintSettingsController : ControllerBase
    {
        private readonly EVEMakeMoneyDbContext _db;

        public BlueprintSettingsController(EVEMakeMoneyDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserBlueprintSettingDto>>> GetSettings()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var settings = await _db.UserBlueprintSettings
                .Where(s => s.UserId == userId)
                .Select(s => new UserBlueprintSettingDto
                {
                    blueprintTypeId = s.BlueprintTypeId,
                    me = s.ME,
                    te = s.TE,
                    updatedAt = s.UpdatedAt
                })
                .ToListAsync();

            return Ok(settings);
        }

        [HttpGet("{blueprintTypeId}")]
        public async Task<ActionResult<UserBlueprintSettingDto>> GetSetting(long blueprintTypeId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var setting = await _db.UserBlueprintSettings
                .FirstOrDefaultAsync(s => s.UserId == userId && s.BlueprintTypeId == blueprintTypeId);

            if (setting == null)
            {
                return NotFound(new { message = "Setting not found" });
            }

            return Ok(new UserBlueprintSettingDto
            {
                blueprintTypeId = setting.BlueprintTypeId,
                me = setting.ME,
                te = setting.TE,
                updatedAt = setting.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<ActionResult<UserBlueprintSettingDto>> SaveSetting([FromBody] SaveBlueprintSettingRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var existing = await _db.UserBlueprintSettings
                .FirstOrDefaultAsync(s => s.UserId == userId && s.BlueprintTypeId == request.BlueprintTypeId);

            if (existing != null)
            {
                existing.ME = request.ME;
                existing.TE = request.TE;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newSetting = new UserBlueprintSettingEntity
                {
                    UserId = userId,
                    BlueprintTypeId = request.BlueprintTypeId,
                    ME = request.ME,
                    TE = request.TE,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.UserBlueprintSettings.Add(newSetting);
            }

            await _db.SaveChangesAsync();

            return Ok(new UserBlueprintSettingDto
            {
                blueprintTypeId = request.BlueprintTypeId,
                me = request.ME,
                te = request.TE,
                updatedAt = DateTime.UtcNow
            });
        }

        [HttpPost("batch")]
        public async Task<ActionResult<List<UserBlueprintSettingDto>>> SaveSettingsBatch([FromBody] List<SaveBlueprintSettingRequest> requests)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            foreach (var request in requests)
            {
                var existing = await _db.UserBlueprintSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.BlueprintTypeId == request.BlueprintTypeId);

                if (existing != null)
                {
                    existing.ME = request.ME;
                    existing.TE = request.TE;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newSetting = new UserBlueprintSettingEntity
                    {
                        UserId = userId,
                        BlueprintTypeId = request.BlueprintTypeId,
                        ME = request.ME,
                        TE = request.TE,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.UserBlueprintSettings.Add(newSetting);
                }
            }

            await _db.SaveChangesAsync();

            var savedSettings = requests.Select(r => new UserBlueprintSettingDto
            {
                blueprintTypeId = r.BlueprintTypeId,
                me = r.ME,
                te = r.TE,
                updatedAt = DateTime.UtcNow
            }).ToList();

            return Ok(savedSettings);
        }

        [HttpDelete("{blueprintTypeId}")]
        public async Task<ActionResult> DeleteSetting(long blueprintTypeId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var setting = await _db.UserBlueprintSettings
                .FirstOrDefaultAsync(s => s.UserId == userId && s.BlueprintTypeId == blueprintTypeId);

            if (setting == null)
            {
                return NotFound(new { message = "Setting not found" });
            }

            _db.UserBlueprintSettings.Remove(setting);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Setting deleted" });
        }
    }

    public class UserBlueprintSettingDto
    {
        public long blueprintTypeId { get; set; }
        public int me { get; set; }
        public int te { get; set; }
        public DateTime updatedAt { get; set; }
    }

    public class SaveBlueprintSettingRequest
    {
        public long BlueprintTypeId { get; set; }
        public int ME { get; set; }
        public int TE { get; set; }
    }
}
