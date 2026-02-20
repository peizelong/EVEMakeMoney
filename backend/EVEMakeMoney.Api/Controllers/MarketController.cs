using Microsoft.AspNetCore.Mvc;
using EVEMakeMoney.Api.Services;
using EVEMakeMoney.Api.Data;
using EVEMakeMoney.Api.Models;

namespace EVEMakeMoney.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarketController : ControllerBase
    {
        private readonly MarketPopularityService _marketPopularityService;
        private readonly TypeNameService _typeNameService;
        private readonly IConfiguration _configuration;

        public MarketController(
            MarketPopularityService marketPopularityService,
            TypeNameService typeNameService,
            IConfiguration configuration)
        {
            _marketPopularityService = marketPopularityService;
            _typeNameService = typeNameService;
            _configuration = configuration;
        }

        [HttpGet("popularity/{typeId}")]
        public ActionResult<MarketPopularityResult> GetPopularity(long typeId, [FromQuery] long regionId = 10000002)
        {
            var sdePath = _configuration["SdePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "sde");
            var typesFilePath = Path.Combine(sdePath, "types.jsonl");
            _typeNameService.Load(typesFilePath);

            var result = _marketPopularityService.Analyze(typeId, regionId);
            return Ok(result);
        }

        [HttpGet("popularity")]
        public ActionResult<List<MarketPopularityResult>> GetAllPopularity([FromQuery] long regionId = 10000002, [FromQuery] int minOrders = 5)
        {
            var sdePath = _configuration["SdePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "sde");
            var typesFilePath = Path.Combine(sdePath, "types.jsonl");
            _typeNameService.Load(typesFilePath);

            var results = _marketPopularityService.AnalyzeAll(regionId, minOrders);
            return Ok(results);
        }

        [HttpGet("popularity/{typeId}/report")]
        public ActionResult<string> GetPopularityReport(long typeId, [FromQuery] long regionId = 10000002)
        {
            var sdePath = _configuration["SdePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "sde");
            var typesFilePath = Path.Combine(sdePath, "types.jsonl");
            _typeNameService.Load(typesFilePath);

            var result = _marketPopularityService.Analyze(typeId, regionId);
            var report = _marketPopularityService.GenerateReport(result);
            return Ok(new { report });
        }
    }
}
