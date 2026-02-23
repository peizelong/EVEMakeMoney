using System.Collections.Generic;

namespace EVEMakeMoney.Api.Models
{
    public class CostBreakdownItem
    {
        public long TypeId { get; set; }
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public decimal UnitTime { get; set; }
        public decimal TotalTime { get; set; }
        public bool IsIntermediate { get; set; }
        public List<CostBreakdownItem> Children { get; set; } = new();
        public decimal? InventionCost { get; set; }
    }
}
