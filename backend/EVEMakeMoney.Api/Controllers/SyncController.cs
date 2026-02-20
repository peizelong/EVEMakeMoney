using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EVEMakeMoney.Api.Services;

namespace EVEMakeMoney.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly MarketDataSyncBackgroundService _syncService;

        public SyncController(MarketDataSyncBackgroundService syncService)
        {
            _syncService = syncService;
        }

        [HttpPost("trigger")]
        [Authorize]
        public ActionResult TriggerSync([FromBody] SyncRequest request)
        {
            _syncService.TriggerSync(request.Orders, request.History);
            return Ok(new { message = "Sync triggered successfully" });
        }
    }

    public class SyncRequest
    {
        public bool Orders { get; set; } = true;
        public bool History { get; set; } = false;
    }
}
