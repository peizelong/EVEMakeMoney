using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EVEMakeMoney.Api.Models;
using EVEMakeMoney.Api.Services;
using EVEMakeMoney.Api.Data;

namespace EVEMakeMoney.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlueprintsController : ControllerBase
    {
        private readonly BlueprintCacheService _blueprintCache;
        private readonly TypeNameService _typeNameService;
        private readonly CostCalculationService _costCalculationService;
        private readonly CostBreakdownService _costBreakdownService;
        private readonly EVEMakeMoneyDbContext _db;

        public BlueprintsController(
            BlueprintCacheService blueprintCache,
            TypeNameService typeNameService,
            CostCalculationService costCalculationService,
            CostBreakdownService costBreakdownService,
            EVEMakeMoneyDbContext db)
        {
            _blueprintCache = blueprintCache;
            _typeNameService = typeNameService;
            _costCalculationService = costCalculationService;
            _costBreakdownService = costBreakdownService;
            _db = db;
        }

        [HttpGet]
        public ActionResult<List<Blueprint>> GetBlueprints([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            var blueprints = _blueprintCache.GetBlueprints();

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                blueprints = blueprints.Where(bp => 
                    bp.Name.ToLower().Contains(searchLower) ||
                    bp.BlueprintTypeId.ToString().Contains(search)
                ).ToList();
            }

            var total = blueprints.Count;
            var pagedBlueprints = blueprints
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            Response.Headers["X-Total-Count"] = total.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();

            return Ok(pagedBlueprints);
        }

        [HttpGet("{blueprintTypeId}")]
        public ActionResult<Blueprint> GetBlueprint(long blueprintTypeId)
        {
            var blueprints = _blueprintCache.GetBlueprints();
            var blueprint = blueprints.FirstOrDefault(bp => bp.BlueprintTypeId == blueprintTypeId);
            
            if (blueprint == null)
            {
                return NotFound(new { error = "Blueprint not found" });
            }

            return Ok(blueprint);
        }

        [HttpPost("calculate")]
        public async Task<ActionResult<List<Blueprint>>> CalculateCosts([FromBody] CalculateCostsRequest request)
        {
            var blueprints = _blueprintCache.GetBlueprints();

            var marketPrices = _costBreakdownService.GetMarketPrices();
            _costBreakdownService.CalculateInventionCosts(blueprints, marketPrices);

            var results = _costCalculationService.CalculateAllCostsAndTimes(
                blueprints,
                request.ME,
                request.TE,
                request.StructureBonus,
                request.RigBonus,
                request.IndustryLevel,
                request.AdvancedIndustryLevel,
                request.ReactionStructureBonus,
                request.ReactionRigBonus,
                request.ReactionLevel
            );

            foreach (var bp in blueprints)
            {
                if (results.TryGetValue(bp.BlueprintTypeId, out var result))
                {
                    bp.ManufacturingCost = result.Cost;
                    bp.ManufacturingTime = result.Time;
                }
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId > 0)
                {
                    var history = new CalculationHistoryEntity
                    {
                        UserId = userId,
                        BlueprintTypeId = 0,
                        ME = request.ME,
                        TE = request.TE,
                        StructureBonus = request.StructureBonus,
                        RigBonus = request.RigBonus,
                        IndustryLevel = request.IndustryLevel,
                        AdvancedIndustryLevel = request.AdvancedIndustryLevel,
                        ReactionStructureBonus = request.ReactionStructureBonus,
                        ReactionRigBonus = request.ReactionRigBonus,
                        ReactionLevel = request.ReactionLevel,
                        CalculatedAt = DateTime.UtcNow
                    };
                    _db.CalculationHistory.Add(history);
                    await _db.SaveChangesAsync();
                }
            }

            return Ok(blueprints);
        }

        [HttpGet("{blueprintTypeId}/breakdown")]
        public ActionResult<CostBreakdownItem> GetCostBreakdown(long blueprintTypeId)
        {
            var blueprints = _blueprintCache.GetBlueprints();

            var breakdown = _costBreakdownService.GetCostBreakdown(blueprintTypeId, blueprints);
            
            if (breakdown == null)
            {
                return NotFound(new { error = "Blueprint not found" });
            }

            return Ok(breakdown);
        }

        [Authorize]
        [HttpGet("history")]
        public async Task<ActionResult<List<CalculationHistoryEntity>>> GetCalculationHistory()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var history = await _db.CalculationHistory
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.CalculatedAt)
                .Take(50)
                .ToListAsync();

            return Ok(history);
        }
    }
}
