using System;
using System.Collections.Generic;
using System.Linq;
using EVEMakeMoney.Api.Data;
using EVEMakeMoney.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EVEMakeMoney.Api.Services
{
    public class CalculationCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly EVEMakeMoneyDbContext _db;
        private Dictionary<long, double>? _marketPricesCache;
        private DateTime _marketPricesCacheTime = DateTime.MinValue;
        private readonly TimeSpan _marketPricesCacheDuration = TimeSpan.FromMinutes(5);

        public CalculationCacheService(IMemoryCache cache, EVEMakeMoneyDbContext db)
        {
            _cache = cache;
            _db = db;
        }

        public Dictionary<long, double> GetMarketPrices()
        {
            if (_marketPricesCache != null && DateTime.UtcNow - _marketPricesCacheTime < _marketPricesCacheDuration)
            {
                return _marketPricesCache;
            }

            var prices = new Dictionary<long, double>();

            var buyOrders = _db.MarketOrders
                .Where(x => x.IsBuyOrder == true)
                .GroupBy(x => x.TypeId)
                .Select(g => new
                {
                    TypeId = g.Key,
                    MaxPrice = g.Max(x => x.Price)
                })
                .ToList();

            foreach (var order in buyOrders)
            {
                prices[order.TypeId] = order.MaxPrice;
            }

            _marketPricesCache = prices;
            _marketPricesCacheTime = DateTime.UtcNow;

            return prices;
        }

        public string GetCacheKey(int me, int te, decimal structureBonus, decimal rigBonus,
            int industryLevel, int advancedIndustryLevel,
            decimal reactionStructureBonus, decimal reactionRigBonus, int reactionLevel)
        {
            return $"calc_{me}_{te}_{structureBonus}_{rigBonus}_{industryLevel}_{advancedIndustryLevel}_{reactionStructureBonus}_{reactionRigBonus}_{reactionLevel}";
        }

        public bool TryGetCachedResults(string cacheKey, out Dictionary<long, (decimal Cost, decimal Time)>? results)
        {
            return _cache.TryGetValue(cacheKey, out results);
        }

        public void SetCachedResults(string cacheKey, Dictionary<long, (decimal Cost, decimal Time)> results)
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));

            _cache.Set(cacheKey, results, cacheOptions);
        }

        public void InvalidateMarketPricesCache()
        {
            _marketPricesCache = null;
        }
    }
}
