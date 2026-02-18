using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVEMakeMoney.Data
{
    public class MarketOrderEntity
    {
        [Key]
        public long OrderId { get; set; }

        public long TypeId { get; set; }

        public long RegionId { get; set; }

        public long SystemId { get; set; }

        public long LocationId { get; set; }

        public bool IsBuyOrder { get; set; }

        public double Price { get; set; }

        public long VolumeRemain { get; set; }

        public long VolumeTotal { get; set; }

        public long Duration { get; set; }

        public long MinVolume { get; set; }

        public string? Range { get; set; }

        public DateTime Issued { get; set; }

        public DateTime FetchedAt { get; set; }
    }
}
