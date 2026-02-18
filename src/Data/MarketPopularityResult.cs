using System;
using System.Collections.Generic;

namespace EVEMakeMoney.Data
{
    public class MarketPopularityResult
    {
        public long TypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;

        public long TotalOrders { get; set; }
        public long RecentOrders { get; set; }
        public double RecentOrdersPercentage { get; set; }

        public long TotalSupply { get; set; }
        public long RecentSupply { get; set; }
        public double RecentSupplyPercentage { get; set; }

        public long TotalVolume { get; set; }
        public long RecentVolume { get; set; }
        public double RecentVolumePercentage { get; set; }

        public List<int> HotHours { get; set; } = new();
        public List<IssuedTimeCluster> TimeClusters { get; set; } = new();

        public double PopularityScore { get; set; }
    }
}
