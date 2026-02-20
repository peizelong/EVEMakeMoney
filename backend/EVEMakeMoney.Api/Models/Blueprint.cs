using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EVEMakeMoney.Api.Models
{
    public partial class Blueprint
    {
        [JsonProperty("_key")]
        public long Key { get; set; }

        [JsonProperty("activities")]
        public Activities? Activities { get; set; }

        [JsonProperty("blueprintTypeID")]
        public long BlueprintTypeId { get; set; }

        [JsonProperty("maxProductionLimit")]
        public long MaxProductionLimit { get; set; }

        public string Name { get; set; } = "";
        [JsonProperty("manufacturingCost")]
        public decimal ManufacturingCost { get; set; }
        [JsonProperty("manufacturingTime")]
        public decimal ManufacturingTime { get; set; }
        [JsonProperty("inventionCost")]
        public decimal InventionCost { get; set; }
        [JsonProperty("me")]
        public int ME { get; set; }
        [JsonProperty("te")]
        public int TE { get; set; }
        public decimal StructureTimeBonus { get; set; } = 0;
        public decimal RigTimeBonus { get; set; } = 0;
        public int IndustryLevel { get; set; } = 0;
        public int AdvancedIndustryLevel { get; set; } = 0;
        public decimal ReactionStructureTimeBonus { get; set; } = 0;
        public decimal ReactionRigTimeBonus { get; set; } = 0;
        public int ReactionLevel { get; set; } = 0;

        public static Blueprint FromJson(string json) => JsonConvert.DeserializeObject<Blueprint>(json, Converter.Settings)!;
    }

    public partial class Activities
    {
        [JsonProperty("copying", NullValueHandling = NullValueHandling.Ignore)]
        public Copying? Copying { get; set; }

        [JsonProperty("invention", NullValueHandling = NullValueHandling.Ignore)]
        public Invention? Invention { get; set; }

        [JsonProperty("manufacturing", NullValueHandling = NullValueHandling.Ignore)]
        public Manufacturing? Manufacturing { get; set; }

        [JsonProperty("reaction", NullValueHandling = NullValueHandling.Ignore)]
        public Reaction? Reaction { get; set; }

        [JsonProperty("research_material", NullValueHandling = NullValueHandling.Ignore)]
        public ResearchMaterial? ResearchMaterial { get; set; }

        [JsonProperty("research_time", NullValueHandling = NullValueHandling.Ignore)]
        public ResearchTime? ResearchTime { get; set; }
    }

    public partial class Copying
    {
        [JsonProperty("materials", NullValueHandling = NullValueHandling.Ignore)]
        public CopyingMaterial[]? Materials { get; set; }

        [JsonProperty("skills", NullValueHandling = NullValueHandling.Ignore)]
        public CopyingSkill[]? Skills { get; set; }

        [JsonProperty("time")]
        public long Time { get; set; }
    }

    public partial class CopyingMaterial
    {
        [JsonProperty("quantity")]
        public long Quantity { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    public partial class CopyingSkill
    {
        [JsonProperty("level")]
        public long Level { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    public partial class Invention
    {
        [JsonProperty("materials", NullValueHandling = NullValueHandling.Ignore)]
        public InventionMaterial[]? Materials { get; set; }

        [JsonProperty("products", NullValueHandling = NullValueHandling.Ignore)]
        public InventionProduct[]? Products { get; set; }

        [JsonProperty("skills", NullValueHandling = NullValueHandling.Ignore)]
        public InventionSkill[]? Skills { get; set; }

        [JsonProperty("time")]
        public long Time { get; set; }
    }

    public partial class InventionMaterial
    {
        [JsonProperty("quantity")]
        public long Quantity { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    public partial class InventionProduct
    {
        [JsonProperty("probability", NullValueHandling = NullValueHandling.Ignore)]
        public double? Probability { get; set; }

        [JsonProperty("quantity")]
        public long Quantity { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    public partial class InventionSkill
    {
        [JsonProperty("level")]
        public long Level { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    public partial class Manufacturing
    {
        [JsonProperty("materials", NullValueHandling = NullValueHandling.Ignore)]
        public ManufacturingMaterial[]? Materials { get; set; }

        [JsonProperty("products", NullValueHandling = NullValueHandling.Ignore)]
        public ManufacturingProduct[]? Products { get; set; }

        [JsonProperty("skills", NullValueHandling = NullValueHandling.Ignore)]
        public ManufacturingSkill[]? Skills { get; set; }

        [JsonProperty("time")]
        public long Time { get; set; }
    }

    public partial class ManufacturingMaterial
    {
        [JsonProperty("quantity")]
        public long Quantity { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    public partial class ManufacturingProduct
    {
        [JsonProperty("quantity")]
        public long Quantity { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    public partial class ManufacturingSkill
    {
        [JsonProperty("level")]
        public long Level { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    public partial class Reaction
    {
        [JsonProperty("materials")]
        public ReactionMaterial[]? Materials { get; set; }

        [JsonProperty("products")]
        public ReactionProduct[]? Products { get; set; }

        [JsonProperty("skills")]
        public ReactionSkill[]? Skills { get; set; }

        [JsonProperty("time")]
        public long Time { get; set; }
    }

    public partial class ReactionMaterial
    {
        [JsonProperty("quantity")]
        public long Quantity { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    public partial class ReactionProduct
    {
        [JsonProperty("quantity")]
        public long Quantity { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    public partial class ReactionSkill
    {
        [JsonProperty("level")]
        public long Level { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    public partial class ResearchMaterial
    {
        [JsonProperty("materials", NullValueHandling = NullValueHandling.Ignore)]
        public ResearchMaterialMaterial[]? Materials { get; set; }

        [JsonProperty("skills", NullValueHandling = NullValueHandling.Ignore)]
        public ResearchMaterialSkill[]? Skills { get; set; }

        [JsonProperty("time")]
        public long Time { get; set; }
    }

    public partial class ResearchMaterialMaterial
    {
        [JsonProperty("quantity")]
        public long Quantity { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    public partial class ResearchMaterialSkill
    {
        [JsonProperty("level")]
        public long Level { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    public partial class ResearchTime
    {
        [JsonProperty("materials", NullValueHandling = NullValueHandling.Ignore)]
        public ResearchTimeMaterial[]? Materials { get; set; }

        [JsonProperty("skills", NullValueHandling = NullValueHandling.Ignore)]
        public ResearchTimeSkill[]? Skills { get; set; }

        [JsonProperty("time")]
        public long Time { get; set; }
    }

    public partial class ResearchTimeMaterial
    {
        [JsonProperty("quantity")]
        public long Quantity { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    public partial class ResearchTimeSkill
    {
        [JsonProperty("level")]
        public long Level { get; set; }

        [JsonProperty("typeID")]
        public long TypeId { get; set; }
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
