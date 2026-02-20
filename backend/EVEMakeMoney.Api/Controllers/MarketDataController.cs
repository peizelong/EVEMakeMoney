using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using EVEMakeMoney.Api.Services;
using EVEMakeMoney.Api.Data;

namespace EVEMakeMoney.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarketDataController : ControllerBase
    {
        private readonly MarketDataService _marketDataService;
        private readonly BlueprintCacheService _blueprintCache;
        private readonly EVEMakeMoneyDbContext _db;

        public MarketDataController(
            MarketDataService marketDataService, 
            BlueprintCacheService blueprintCache,
            EVEMakeMoneyDbContext db)
        {
            _marketDataService = marketDataService;
            _blueprintCache = blueprintCache;
            _db = db;
        }

        [HttpGet("status")]
        public async Task<ActionResult<MarketDataStatus>> GetMarketDataStatus()
        {
            var orderCount = await _db.MarketOrders.CountAsync();
            var historyCount = await _db.MarketHistory.CountAsync();
            
            var lastOrderFetch = await _db.MarketOrders
                .OrderByDescending(x => x.FetchedAt)
                .Select(x => x.FetchedAt)
                .FirstOrDefaultAsync();

            var lastHistoryFetch = await _db.MarketHistory
                .OrderByDescending(x => x.FetchedAt)
                .Select(x => x.FetchedAt)
                .FirstOrDefaultAsync();

            return Ok(new MarketDataStatus
            {
                OrderCount = orderCount,
                HistoryCount = historyCount,
                LastOrderFetch = lastOrderFetch == default ? null : lastOrderFetch,
                LastHistoryFetch = lastHistoryFetch == default ? null : lastHistoryFetch,
                HasMarketData = orderCount > 0
            });
        }

        [HttpPost("fetch-orders/{regionId}")]
        [Authorize]
        public async Task<ActionResult> FetchMarketOrders(long regionId, CancellationToken cancellationToken)
        {
            var progress = new Progress<MarketFetchProgress>(p =>
            {
            });

            try
            {
                await _marketDataService.FetchMarketOrdersAsync(regionId, progress, cancellationToken);
                return Ok(new { message = $"Successfully fetched market orders for region {regionId}" });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, new { message = "Request cancelled" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error fetching market data: {ex.Message}" });
            }
        }

        [HttpPost("fetch-history/{regionId}")]
        [Authorize]
        public async Task<ActionResult> FetchMarketHistory(long regionId, CancellationToken cancellationToken)
        {
            try
            {
                var blueprints = _blueprintCache.GetBlueprints();
                var typeIds = new HashSet<long>();

                foreach (var bp in blueprints)
                {
                    if (bp.Activities?.Manufacturing?.Materials != null)
                    {
                        foreach (var material in bp.Activities.Manufacturing.Materials)
                        {
                            typeIds.Add(material.TypeId);
                        }
                    }

                    if (bp.Activities?.Invention?.Materials != null)
                    {
                        foreach (var material in bp.Activities.Invention.Materials)
                        {
                            typeIds.Add(material.TypeId);
                        }
                    }

                    if (bp.Activities?.Reaction?.Materials != null)
                    {
                        foreach (var material in bp.Activities.Reaction.Materials)
                        {
                            typeIds.Add(material.TypeId);
                        }
                    }
                }

                var progress = new Progress<int>(p =>
                {
                });

                await _marketDataService.FetchAllMarketHistoryAsync(regionId, typeIds.ToList(), progress, cancellationToken);

                return Ok(new { message = $"Successfully fetched market history for {typeIds.Count} types in region {regionId}" });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, new { message = "Request cancelled" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error fetching market history: {ex.Message}" });
            }
        }

        [HttpPost("fetch-history/{regionId}/{typeId}")]
        [Authorize]
        public async Task<ActionResult> FetchMarketHistoryForType(long regionId, long typeId, CancellationToken cancellationToken)
        {
            try
            {
                await _marketDataService.FetchMarketHistoryAsync(regionId, typeId, cancellationToken);
                return Ok(new { message = $"Successfully fetched market history for type {typeId}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error fetching market history: {ex.Message}" });
            }
        }

        [HttpPost("calculate-weekly-sales/{regionId}/{typeId}")]
        [Authorize]
        public async Task<ActionResult> CalculateWeeklySales(long regionId, long typeId)
        {
            try
            {
                await _marketDataService.CalculateWeeklySalesAsync(regionId, typeId);
                return Ok(new { message = $"Successfully calculated weekly sales for type {typeId}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error calculating weekly sales: {ex.Message}" });
            }
        }

        [HttpGet("prices")]
        public async Task<ActionResult<Dictionary<long, double>>> GetMarketPrices()
        {
            var prices = await _marketDataService.GetMarketPricesAsync();
            return Ok(prices);
        }
    }

    public class MarketDataStatus
    {
        public int OrderCount { get; set; }
        public int HistoryCount { get; set; }
        public DateTime? LastOrderFetch { get; set; }
        public DateTime? LastHistoryFetch { get; set; }
        public bool HasMarketData { get; set; }
    }
}
