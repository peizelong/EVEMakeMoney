namespace EVEMakeMoney.Api.Models
{
    public class CalculateCostsRequest
    {
        public int ME { get; set; } = 0;
        public int TE { get; set; } = 0;
        public decimal StructureBonus { get; set; } = 0;
        public decimal RigBonus { get; set; } = 0;
        public int IndustryLevel { get; set; } = 0;
        public int AdvancedIndustryLevel { get; set; } = 0;
        public decimal ReactionStructureBonus { get; set; } = 0;
        public decimal ReactionRigBonus { get; set; } = 0;
        public int ReactionLevel { get; set; } = 0;
    }
}
