using EVEMakeMoney.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace EVEMakeMoney.Api.Services
{
    public class BlueprintCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly BlueprintService _blueprintService;
        private readonly TypeNameService _typeNameService;
        private readonly IConfiguration _configuration;
        private const string CacheKey = "AllBlueprints";

        public BlueprintCacheService(
            IMemoryCache cache,
            BlueprintService blueprintService,
            TypeNameService typeNameService,
            IConfiguration configuration)
        {
            _cache = cache;
            _blueprintService = blueprintService;
            _typeNameService = typeNameService;
            _configuration = configuration;
        }

        public List<Blueprint> GetBlueprints()
        {
            if (_cache.TryGetValue(CacheKey, out List<Blueprint>? cachedBlueprints))
            {
                return cachedBlueprints!;
            }

            var sdePath = _configuration["SdePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "sde");
            var filePath = Path.Combine(sdePath, "blueprints.jsonl");
            var typesFilePath = Path.Combine(sdePath, "types.jsonl");

            _typeNameService.Load(typesFilePath);

            var blueprints = _blueprintService.LoadBlueprints(filePath);
            
            foreach (var bp in blueprints)
            {
                bp.Name = _typeNameService.GetName(bp.BlueprintTypeId);
            }

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1))
                .SetAbsoluteExpiration(TimeSpan.FromHours(24));

            _cache.Set(CacheKey, blueprints, cacheOptions);

            return blueprints;
        }

        public void InvalidateCache()
        {
            _cache.Remove(CacheKey);
        }
    }
}
