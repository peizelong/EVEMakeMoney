using System.Text.Json;
using System.Text.Json.Serialization;
using EVEMakeMoney.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EVEMakeMoney.Api.Services
{
    public enum AssetLocationType
    {
        Container,
        AssetSafety,
        System,
        AbyssalSystem,
        Station,
        Structure,
        Other
    }

    public enum AssetOwnerType
    {
        Character,
        Corporation
    }

    public class AssetService
    {
        private readonly EVEMakeMoneyDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private const string ESI_BASE_URL = "https://esi.evetech.net/v6";

        public AssetService(EVEMakeMoneyDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(10);
        }

        public async Task<List<AssetItem>> GetCharacterAssetsAsync(long characterId, string accessToken)
        {
            var assets = new List<AssetItem>();
            var page = 1;

            while (true)
            {
                var url = $"{ESI_BASE_URL}/characters/{characterId}/assets/?page={page}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to fetch assets: {response.StatusCode}");
                }

                var items = JsonSerializer.Deserialize<List<AssetItem>>(await response.Content.ReadAsStringAsync());
                if (items == null || items.Count == 0)
                    break;

                assets.AddRange(items);

                var totalPages = 1;
                if (response.Headers.TryGetValues("X-Pages", out var pages))
                {
                    if (int.TryParse(pages.FirstOrDefault(), out var total))
                        totalPages = total;
                }

                if (page >= totalPages)
                    break;

                page++;
            }

            return assets;
        }

        public async Task<List<AssetItem>> GetCorporationAssetsAsync(long characterId, long corporationId, string accessToken)
        {
            var assets = new List<AssetItem>();
            var page = 1;

            while (true)
            {
                var url = $"{ESI_BASE_URL}/corporations/{corporationId}/assets/?page={page}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return assets;
                }

                var items = JsonSerializer.Deserialize<List<AssetItem>>(await response.Content.ReadAsStringAsync());
                if (items == null || items.Count == 0)
                    break;

                assets.AddRange(items);

                var totalPages = 1;
                if (response.Headers.TryGetValues("X-Pages", out var pages))
                {
                    if (int.TryParse(pages.FirstOrDefault(), out var total))
                        totalPages = total;
                }

                if (page >= totalPages)
                    break;

                page++;
            }

            return assets;
        }

        public async Task<Dictionary<long, string>> GetCharacterItemNamesAsync(long characterId, string accessToken, List<long> itemIds)
        {
            if (itemIds.Count == 0)
                return new Dictionary<long, string>();

            var url = $"{ESI_BASE_URL}/characters/{characterId}/assets/names/";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            request.Content = JsonContent.Create(itemIds);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return new Dictionary<long, string>();

            var names = JsonSerializer.Deserialize<List<ItemName>>(await response.Content.ReadAsStringAsync());
            return names?.ToDictionary(n => n.ItemId, n => n.Name) ?? new Dictionary<long, string>();
        }

        public async Task<Dictionary<long, string>> GetCorporationItemNamesAsync(long characterId, long corporationId, string accessToken, List<long> itemIds)
        {
            if (itemIds.Count == 0)
                return new Dictionary<long, string>();

            var url = $"{ESI_BASE_URL}/corporations/{corporationId}/assets/names/";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            request.Content = JsonContent.Create(itemIds);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return new Dictionary<long, string>();

            var names = JsonSerializer.Deserialize<List<ItemName>>(await response.Content.ReadAsStringAsync());
            return names?.ToDictionary(n => n.ItemId, n => n.Name) ?? new Dictionary<long, string>();
        }

        public async Task<UniverseStationInfo?> GetStationInfoAsync(long stationId)
        {
            try
            {
                var url = $"{ESI_BASE_URL}/universe/stations/{stationId}/";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return null;

                return JsonSerializer.Deserialize<UniverseStationInfo>(await response.Content.ReadAsStringAsync());
            }
            catch
            {
                return null;
            }
        }

        public async Task<UniverseStructureInfo?> GetStructureInfoAsync(long structureId, long characterId, string accessToken)
        {
            try
            {
                var url = $"{ESI_BASE_URL}/universe/structures/{structureId}/";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                return JsonSerializer.Deserialize<UniverseStructureInfo>(await response.Content.ReadAsStringAsync());
            }
            catch
            {
                return null;
            }
        }

        public async Task<Dictionary<long, string>> GetUniverseNamesAsync(List<long> ids)
        {
            if (ids.Count == 0)
                return new Dictionary<long, string>();

            try
            {
                var url = $"{ESI_BASE_URL}/universe/names/";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = JsonContent.Create(ids);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new Dictionary<long, string>();

                var names = JsonSerializer.Deserialize<List<UniverseName>>(await response.Content.ReadAsStringAsync());
                return names?.ToDictionary(n => n.Id, n => n.Name) ?? new Dictionary<long, string>();
            }
            catch
            {
                return new Dictionary<long, string>();
            }
        }

        public async Task<Dictionary<int, TypeInfo>> GetTypeInfosAsync(List<int> typeIds)
        {
            if (typeIds.Count == 0)
                return new Dictionary<int, TypeInfo>();

            var typeInfos = new Dictionary<int, TypeInfo>();
            var uniqueTypeIds = typeIds.Distinct().ToList();

            var tasks = new List<Task<(int typeId, TypeInfo? info)>>();
            foreach (var typeId in uniqueTypeIds)
            {
                tasks.Add(GetTypeInfoAsync(typeId));
            }

            var results = await Task.WhenAll(tasks);
            foreach (var result in results)
            {
                if (result.info != null)
                {
                    typeInfos[result.typeId] = result.info;
                }
            }

            return typeInfos;
        }

        private async Task<(int typeId, TypeInfo? info)> GetTypeInfoAsync(int typeId)
        {
            try
            {
                var url = $"{ESI_BASE_URL}/universe/types/{typeId}/?language=zh";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return (typeId, null);

                var info = JsonSerializer.Deserialize<TypeInfo>(await response.Content.ReadAsStringAsync());
                return (typeId, info);
            }
            catch
            {
                return (typeId, null);
            }
        }

        public async Task<Dictionary<int, double>> GetMarketPricesAsync(List<int> typeIds)
        {
            if (typeIds.Count == 0)
                return new Dictionary<int, double>();

            try
            {
                var url = $"{ESI_BASE_URL}/markets/prices/";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return new Dictionary<int, double>();

                var prices = JsonSerializer.Deserialize<List<MarketPrice>>(await response.Content.ReadAsStringAsync());
                return prices?
                    .Where(p => typeIds.Contains(p.TypeId))
                    .ToDictionary(p => p.TypeId, p => p.AveragePrice ?? 0)
                    ?? new Dictionary<int, double>();
            }
            catch
            {
                return new Dictionary<int, double>();
            }
        }

        public async Task<Dictionary<int, Dictionary<int, string>>> GetCorporationDivisionsAsync(long characterId, long corporationId, string accessToken)
        {
            try
            {
                var url = $"{ESI_BASE_URL}/corporations/{corporationId}/divisions/";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new Dictionary<int, Dictionary<int, string>>();

                var divisions = JsonSerializer.Deserialize<CorporationDivisions>(await response.Content.ReadAsStringAsync());
                var result = new Dictionary<int, Dictionary<int, string>>();

                if (divisions?.HangarDivisions != null)
                {
                    result[0] = divisions.HangarDivisions
                        .Where(d => d.Id.HasValue && !string.IsNullOrEmpty(d.Name))
                        .ToDictionary(d => d.Id!.Value, d => d.Name!);
                }

                return result;
            }
            catch
            {
                return new Dictionary<int, Dictionary<int, string>>();
            }
        }

        public async Task<List<AssetWithLocation>> GetAllAssetsWithLocationsAsync(CharacterEntity character)
        {
            if (string.IsNullOrEmpty(character.AccessToken))
                throw new Exception("No access token for character");

            var characterAssets = await GetCharacterAssetsAsync(character.CharacterId, character.AccessToken);

            var hasCorporationAssetsScope = !string.IsNullOrEmpty(character.Scopes) &&
                character.Scopes.Contains("esi-assets.read_corporation_assets.v1");

            List<AssetItem> corporationAssets = new();
            if (hasCorporationAssetsScope && character.CorporationId.HasValue)
            {
                var isDirector = await GetCharacterCorporationRolesAsync(character.CharacterId, character.CorporationId.Value, character.AccessToken);
                if (isDirector)
                {
                    corporationAssets = await GetCorporationAssetsAsync(character.CharacterId, character.CorporationId.Value, character.AccessToken);
                }
            }

            var allAssets = characterAssets
                .Select(a => new AssetWithOwner { Asset = a, OwnerType = AssetOwnerType.Character, OwnerId = character.CharacterId })
                .ToList();

            if (corporationAssets.Any())
            {
                allAssets.AddRange(corporationAssets.Select(a => new AssetWithOwner
                {
                    Asset = a,
                    OwnerType = AssetOwnerType.Corporation,
                    OwnerId = character.CorporationId ?? 0,
                    CorporationId = character.CorporationId
                }));
            }

            if (!allAssets.Any())
                return new List<AssetWithLocation>();

            var itemIds = allAssets.Select(a => a.Asset.ItemId).Distinct().ToList();
            var resolvedLocations = await ResolveAssetLocationsAsync(allAssets, itemIds, character);

            var typeIds = allAssets.Select(a => a.Asset.TypeId).Distinct().ToList();
            var typeInfos = await GetTypeInfosAsync(typeIds);
            var prices = await GetMarketPricesAsync(typeIds);

            var nameableItemIds = allAssets.Where(a => a.Asset.IsSingleton && a.Asset.ItemId > 0).Select(a => a.Asset.ItemId).ToList();
            Dictionary<long, string> itemNames = new();
            if (nameableItemIds.Any())
            {
                if (characterAssets.Any())
                {
                    var charNames = await GetCharacterItemNamesAsync(character.CharacterId, character.AccessToken, nameableItemIds);
                    foreach (var kvp in charNames)
                    {
                        if (kvp.Value != "None")
                            itemNames[kvp.Key] = kvp.Value;
                    }
                }

                if (corporationAssets.Any() && character.CorporationId.HasValue)
                {
                    var corpNames = await GetCorporationItemNamesAsync(character.CharacterId, character.CorporationId.Value, character.AccessToken, nameableItemIds);
                    foreach (var kvp in corpNames)
                    {
                        if (kvp.Value != "None" && !itemNames.ContainsKey(kvp.Key))
                            itemNames[kvp.Key] = kvp.Value;
                    }
                }
            }

            var corporationDivisions = character.CorporationId.HasValue && corporationAssets.Any()
                ? await GetCorporationDivisionsAsync(character.CharacterId, character.CorporationId.Value, character.AccessToken)
                : new Dictionary<int, Dictionary<int, string>>();

            var result = new List<AssetWithLocation>();
            foreach (var assetWithOwner in allAssets)
            {
                var asset = assetWithOwner.Asset;
                typeInfos.TryGetValue(asset.TypeId, out var typeInfo);
                prices.TryGetValue(asset.TypeId, out var price);

                var locationType = GetLocationType(asset.LocationId, asset.LocationType, itemIds);
                var location = GetAssetLocation(asset, locationType, resolvedLocations, itemIds, allAssets);

                var isContainer = locationType == AssetLocationType.Container;
                var name = !isContainer && itemNames.TryGetValue(asset.ItemId, out var itemName) ? itemName : null;

                result.Add(new AssetWithLocation
                {
                    ItemId = asset.ItemId,
                    TypeId = asset.TypeId,
                    Quantity = asset.Quantity,
                    LocationId = asset.LocationId,
                    LocationFlag = asset.LocationFlag ?? "",
                    IsSingleton = asset.IsSingleton,
                    LocationType = locationType,
                    OwnerType = assetWithOwner.OwnerType,
                    OwnerId = assetWithOwner.OwnerId,
                    CorporationId = assetWithOwner.CorporationId,
                    Name = name,
                    TypeName = typeInfo?.Name ?? $"TypeID: {asset.TypeId}",
                    GroupName = typeInfo?.GroupName,
                    CategoryName = typeInfo?.CategoryName,
                    Volume = typeInfo?.Volume ?? 0,
                    IconUrl = $"https://images.evetech.net/types/{asset.TypeId}/icon?size=64",
                    GraphicUrl = $"https://images.evetech.net/types/{asset.TypeId}/graphic?size=64",
                    EstimatedValue = price * asset.Quantity,
                    UnitPrice = price,
                    ResolvedLocation = location
                });
            }

            return result;
        }

        private async Task<bool> GetCharacterCorporationRolesAsync(long characterId, long corporationId, string accessToken)
        {
            try
            {
                var url = $"{ESI_BASE_URL}/characters/{characterId}/corporationroles/";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return false;

                var roles = JsonSerializer.Deserialize<List<string>>(await response.Content.ReadAsStringAsync());
                return roles?.Any(r => r.Contains("Director")) ?? false;
            }
            catch
            {
                return false;
            }
        }

        private AssetLocationType GetLocationType(long locationId, string? locationTypeStr, List<long> itemIds)
        {
            if (locationId == 2004)
                return AssetLocationType.AssetSafety;

            var locationTypeEnum = locationTypeStr?.ToLower() switch
            {
                "solarsystem" => "SolarSystem",
                "station" => "Station",
                "item" => "Item",
                _ => locationTypeStr
            };

            if (locationTypeEnum == "SolarSystem")
            {
                if (locationId >= 30000000 && locationId <= 32000000)
                    return AssetLocationType.System;
                if (locationId >= 32000000 && locationId <= 33000000)
                    return AssetLocationType.AbyssalSystem;
                return AssetLocationType.System;
            }

            if (locationTypeEnum == "Station")
                return AssetLocationType.Station;

            if (locationTypeEnum == "Item")
            {
                if (itemIds.Contains(locationId))
                    return AssetLocationType.Container;
                return AssetLocationType.Structure;
            }

            return AssetLocationType.Other;
        }

        private async Task<ResolvedAssetLocations> ResolveAssetLocationsAsync(List<AssetWithOwner> allAssets, List<long> itemIds, CharacterEntity character)
        {
            var stationIds = new List<long>();
            var structureIds = new List<long>();

            foreach (var asset in allAssets)
            {
                var locationType = GetLocationType(asset.Asset.LocationId, asset.Asset.LocationType, itemIds);
                switch (locationType)
                {
                    case AssetLocationType.Station:
                        if (!stationIds.Contains(asset.Asset.LocationId))
                            stationIds.Add(asset.Asset.LocationId);
                        break;
                    case AssetLocationType.Structure:
                        if (!structureIds.Contains(asset.Asset.LocationId))
                            structureIds.Add(asset.Asset.LocationId);
                        break;
                }
            }

            var stationsTask = stationIds.Select(async id =>
            {
                var info = await GetStationInfoAsync(id);
                return (id, info);
            });
            var stationResults = await Task.WhenAll(stationsTask);
            var stationsById = stationResults
                .Where(r => r.info != null)
                .ToDictionary(r => r.id, r => r.info!);

            var structuresTask = structureIds.Select(async id =>
            {
                var info = await GetStructureInfoAsync(id, character.CharacterId, character.AccessToken ?? "");
                return (id, info);
            });
            var structureResults = await Task.WhenAll(structuresTask);
            var structuresById = structureResults
                .Where(r => r.info != null)
                .ToDictionary(r => r.id, r => r.info!);

            return new ResolvedAssetLocations
            {
                StationsById = stationsById,
                StructuresById = structuresById
            };
        }

        private ResolvedLocation? GetAssetLocation(AssetItem asset, AssetLocationType locationType, ResolvedAssetLocations resolved, List<long> itemIds, List<AssetWithOwner> allAssets)
        {
            var locationId = asset.LocationId;

            return locationType switch
            {
                AssetLocationType.Container => null,
                AssetLocationType.AssetSafety => new ResolvedLocation
                {
                    LocationId = locationId,
                    Name = "Asset Safety",
                    TypeId = null,
                    SystemId = null,
                    LocationTypeName = "AssetSafety"
                },
                AssetLocationType.System or AssetLocationType.AbyssalSystem => new ResolvedLocation
                {
                    LocationId = locationId,
                    Name = $"System {locationId}",
                    TypeId = null,
                    SystemId = (int)locationId,
                    LocationTypeName = "System"
                },
                AssetLocationType.Station when resolved.StationsById.TryGetValue(locationId, out var station) => new ResolvedLocation
                {
                    LocationId = locationId,
                    Name = station.Name ?? $"Station {locationId}",
                    TypeId = station.TypeId,
                    SystemId = station.SystemId,
                    LocationTypeName = "Station"
                },
                AssetLocationType.Structure when resolved.StructuresById.TryGetValue(locationId, out var structure) => new ResolvedLocation
                {
                    LocationId = locationId,
                    Name = structure.Name ?? $"Structure {locationId}",
                    TypeId = structure.TypeId,
                    SystemId = structure.SolarSystemId,
                    LocationTypeName = "Structure"
                },
                _ => new ResolvedLocation
                {
                    LocationId = locationId,
                    Name = $"Unknown {locationId}",
                    TypeId = null,
                    SystemId = null,
                    LocationTypeName = "Unknown"
                }
            };
        }

        public async Task<List<AssetTreeNode>> GetAssetsAsTreeAsync(CharacterEntity character)
        {
            var assetsWithLocation = await GetAllAssetsWithLocationsAsync(character);

            var itemIds = assetsWithLocation.Select(a => a.ItemId).ToHashSet();

            var rootAssets = assetsWithLocation
                .Where(a => !itemIds.Contains(a.LocationId))
                .ToList();

            var tree = new List<AssetTreeNode>();
            foreach (var asset in rootAssets)
            {
                tree.Add(BuildAssetTree(asset, assetsWithLocation, itemIds));
            }

            return tree.OrderBy(a => a.LocationFlag)
                .ThenBy(a => a.Name)
                .ToList();
        }

        private AssetTreeNode BuildAssetTree(AssetWithLocation asset, List<AssetWithLocation> allAssets, HashSet<long> itemIds)
        {
            var children = allAssets
                .Where(a => a.LocationId == asset.ItemId)
                .Select(a => BuildAssetTree(a, allAssets, itemIds))
                .OrderBy(a => a.LocationFlag)
                .ThenBy(a => a.Name)
                .ToList();

            return new AssetTreeNode
            {
                ItemId = asset.ItemId,
                TypeId = asset.TypeId,
                Quantity = asset.Quantity,
                LocationId = asset.LocationId,
                LocationFlag = asset.LocationFlag,
                IsSingleton = asset.IsSingleton,
                Name = asset.Name,
                TypeName = asset.TypeName,
                GroupName = asset.GroupName,
                CategoryName = asset.CategoryName,
                Volume = asset.Volume,
                IconUrl = asset.IconUrl,
                GraphicUrl = asset.GraphicUrl,
                EstimatedValue = asset.EstimatedValue,
                UnitPrice = asset.UnitPrice,
                OwnerType = asset.OwnerType,
                OwnerId = asset.OwnerId,
                ResolvedLocation = asset.ResolvedLocation,
                Children = children
            };
        }

        public async Task<List<AssetGroupedByResolvedLocation>> GetAssetsGroupedByResolvedLocationAsync(CharacterEntity character)
        {
            var tree = await GetAssetsAsTreeAsync(character);

            var result = new List<AssetGroupedByResolvedLocation>();

            var locationGroups = tree
                .SelectMany(FlattenTree)
                .Where(a => a.ResolvedLocation != null)
                .GroupBy(a => a.ResolvedLocation!.LocationId);

            foreach (var group in locationGroups)
            {
                var firstAsset = group.First();
                result.Add(new AssetGroupedByResolvedLocation
                {
                    LocationId = group.Key,
                    LocationName = firstAsset.ResolvedLocation?.Name ?? "Unknown",
                    LocationType = firstAsset.ResolvedLocation?.LocationTypeName ?? "Unknown",
                    SystemId = firstAsset.ResolvedLocation?.SystemId,
                    Assets = group.ToList(),
                    TotalValue = group.Sum(a => a.EstimatedValue),
                    ItemCount = group.Sum(a => a.Quantity)
                });
            }

            return result.OrderByDescending(l => l.TotalValue).ToList();
        }

        private IEnumerable<AssetTreeNode> FlattenTree(AssetTreeNode node)
        {
            yield return node;
            foreach (var child in node.Children)
            {
                foreach (var descendant in FlattenTree(child))
                {
                    yield return descendant;
                }
            }
        }

        public async Task<AssetSummaryResponse> GetAssetSummaryAsync(CharacterEntity character)
        {
            var tree = await GetAssetsAsTreeAsync(character);

            var allAssets = tree.SelectMany(FlattenTree).ToList();

            return new AssetSummaryResponse
            {
                TotalItems = allAssets.Count,
                TotalValue = allAssets.Sum(a => a.EstimatedValue),
                TotalVolume = allAssets.Sum(a => a.Volume * a.Quantity),
                LocationCount = allAssets
                    .Where(a => a.ResolvedLocation != null)
                    .GroupBy(a => a.ResolvedLocation!.LocationId)
                    .Count()
            };
        }
    }

    public class AssetWithOwner
    {
        public AssetItem Asset { get; set; } = new();
        public AssetOwnerType OwnerType { get; set; }
        public long OwnerId { get; set; }
        public long? CorporationId { get; set; }
    }

    public class AssetWithLocation
    {
        public long ItemId { get; set; }
        public int TypeId { get; set; }
        public int Quantity { get; set; }
        public long LocationId { get; set; }
        public string LocationFlag { get; set; } = "";
        public bool IsSingleton { get; set; }
        public AssetLocationType LocationType { get; set; }
        public AssetOwnerType OwnerType { get; set; }
        public long OwnerId { get; set; }
        public long? CorporationId { get; set; }
        public string? Name { get; set; }
        public string TypeName { get; set; } = "";
        public string? GroupName { get; set; }
        public string? CategoryName { get; set; }
        public double Volume { get; set; }
        public string? IconUrl { get; set; }
        public string? GraphicUrl { get; set; }
        public double EstimatedValue { get; set; }
        public double UnitPrice { get; set; }
        public ResolvedLocation? ResolvedLocation { get; set; }
    }

    public class ResolvedLocation
    {
        public long LocationId { get; set; }
        public string Name { get; set; } = "";
        public int? TypeId { get; set; }
        public int? SystemId { get; set; }
        public string LocationTypeName { get; set; } = "";
    }

    public class ResolvedAssetLocations
    {
        public Dictionary<long, UniverseStationInfo> StationsById { get; set; } = new();
        public Dictionary<long, UniverseStructureInfo> StructuresById { get; set; } = new();
    }

    public class AssetTreeNode
    {
        public long ItemId { get; set; }
        public int TypeId { get; set; }
        public int Quantity { get; set; }
        public long LocationId { get; set; }
        public string LocationFlag { get; set; } = "";
        public bool IsSingleton { get; set; }
        public string? Name { get; set; }
        public string TypeName { get; set; } = "";
        public string? GroupName { get; set; }
        public string? CategoryName { get; set; }
        public double Volume { get; set; }
        public string? IconUrl { get; set; }
        public string? GraphicUrl { get; set; }
        public double EstimatedValue { get; set; }
        public double UnitPrice { get; set; }
        public AssetOwnerType OwnerType { get; set; }
        public long OwnerId { get; set; }
        public ResolvedLocation? ResolvedLocation { get; set; }
        public List<AssetTreeNode> Children { get; set; } = new();
    }

    public class AssetGroupedByResolvedLocation
    {
        public long LocationId { get; set; }
        public string LocationName { get; set; } = "";
        public string LocationType { get; set; } = "";
        public int? SystemId { get; set; }
        public List<AssetTreeNode> Assets { get; set; } = new();
        public double TotalValue { get; set; }
        public int ItemCount { get; set; }
    }

    public class AssetSummaryResponse
    {
        public int TotalItems { get; set; }
        public double TotalValue { get; set; }
        public double TotalVolume { get; set; }
        public int LocationCount { get; set; }
    }

    public class AssetItem
    {
        [JsonPropertyName("item_id")]
        public long ItemId { get; set; }

        [JsonPropertyName("type_id")]
        public int TypeId { get; set; }

        [JsonPropertyName("location_id")]
        public long LocationId { get; set; }

        [JsonPropertyName("location_flag")]
        public string? LocationFlag { get; set; }

        [JsonPropertyName("location_type")]
        public string? LocationType { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("is_singleton")]
        public bool IsSingleton { get; set; }
    }

    public class ItemName
    {
        [JsonPropertyName("item_id")]
        public long ItemId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    public class UniverseStationInfo
    {
        [JsonPropertyName("station_id")]
        public long StationId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type_id")]
        public int? TypeId { get; set; }

        [JsonPropertyName("system_id")]
        public int? SystemId { get; set; }
    }

    public class UniverseStructureInfo
    {
        [JsonPropertyName("structure_id")]
        public long StructureId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type_id")]
        public int? TypeId { get; set; }

        [JsonPropertyName("solar_system_id")]
        public int? SolarSystemId { get; set; }
    }

    public class UniverseName
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("category")]
        public string Category { get; set; } = "";
    }

    public class TypeInfo
    {
        [JsonPropertyName("type_id")]
        public int TypeId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("group_id")]
        public int? GroupId { get; set; }

        [JsonPropertyName("volume")]
        public double? Volume { get; set; }

        [JsonPropertyName("published")]
        public bool Published { get; set; }

        [JsonPropertyName("group_name")]
        public string? GroupName { get; set; }

        [JsonPropertyName("category_name")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("market_group_id")]
        public int? MarketGroupId { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("mass")]
        public double? Mass { get; set; }

        [JsonPropertyName("packaged_volume")]
        public double? PackagedVolume { get; set; }

        [JsonPropertyName("capacity")]
        public double? Capacity { get; set; }
    }

    public class MarketPrice
    {
        [JsonPropertyName("type_id")]
        public int TypeId { get; set; }

        [JsonPropertyName("average_price")]
        public double? AveragePrice { get; set; }

        [JsonPropertyName("adjusted_price")]
        public double? AdjustedPrice { get; set; }
    }

    public class CorporationDivisions
    {
        [JsonPropertyName("hangar")]
        public List<HangarDivision>? HangarDivisions { get; set; }

        [JsonPropertyName("wallet")]
        public List<WalletDivision>? WalletDivisions { get; set; }
    }

    public class HangarDivision
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class WalletDivision
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
