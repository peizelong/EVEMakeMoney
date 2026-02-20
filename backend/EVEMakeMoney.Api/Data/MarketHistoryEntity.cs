using System.ComponentModel.DataAnnotations;

namespace EVEMakeMoney.Api.Data
{
    public class MarketHistoryEntity
    {
        [Key]
        public long Id { get; set; }

        public long TypeId { get; set; }

        public long RegionId { get; set; }

        public DateTime Date { get; set; }

        public double Average { get; set; }

        public double Highest { get; set; }

        public double Lowest { get; set; }

        public long OrderCount { get; set; }

        public long Volume { get; set; }

        public DateTime FetchedAt { get; set; }
    }
}
