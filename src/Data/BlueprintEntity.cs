using System.ComponentModel.DataAnnotations;

namespace EVEMakeMoney.Data
{
    public class BlueprintEntity
    {
        [Key]
        public long BlueprintTypeId { get; set; }

        public int ME { get; set; }

        public int TE { get; set; }
    }
}
