using System.ComponentModel.DataAnnotations;

namespace EVEMakeMoney.Api.Data
{
    public class CalculationHistoryEntity
    {
        [Key]
        public long Id { get; set; }
        
        public int UserId { get; set; }
        
        public long BlueprintTypeId { get; set; }
        
        public int ME { get; set; }
        
        public int TE { get; set; }
        
        public decimal StructureBonus { get; set; }
        
        public decimal RigBonus { get; set; }
        
        public int IndustryLevel { get; set; }
        
        public int AdvancedIndustryLevel { get; set; }
        
        public decimal ReactionStructureBonus { get; set; }
        
        public decimal ReactionRigBonus { get; set; }
        
        public int ReactionLevel { get; set; }
        
        public decimal ManufacturingCost { get; set; }
        
        public decimal ManufacturingTime { get; set; }
        
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}
