using System.ComponentModel.DataAnnotations;

namespace EVEMakeMoney.Api.Data
{
    public class UserBlueprintSettingEntity
    {
        [Key]
        public long Id { get; set; }
        
        public int UserId { get; set; }
        
        public long BlueprintTypeId { get; set; }
        
        public int ME { get; set; }
        
        public int TE { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
