namespace EVEMakeMoney.Api.DTOs.Blueprints;

public class BlueprintSettingDto
{
    public int BlueprintTypeId { get; set; }
    public int Me { get; set; }
    public int Te { get; set; }
}

public class BlueprintSettingBatchRequest
{
    public List<BlueprintSettingDto> Settings { get; set; } = new();
}
