namespace EVEMakeMoney.Api.DTOs.Assets;

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
