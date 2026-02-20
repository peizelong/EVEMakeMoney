using System.ComponentModel.DataAnnotations;

namespace EVEMakeMoney.Api.Data
{
    public class WeeklySalesEntity
    {
        [Key]
        public long Id { get; set; }

        public long TypeId { get; set; }

        public long RegionId { get; set; }

        public int Year { get; set; }

        public int WeekNumber { get; set; }

        public long TotalVolume { get; set; }

        public double AveragePrice { get; set; }

        public double HighestPrice { get; set; }

        public double LowestPrice { get; set; }

        public long OrderCount { get; set; }

        public DateTime WeekStart { get; set; }

        public DateTime WeekEnd { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
