using System;
using System.Collections.Generic;
using System.Linq;
using EVEMakeMoney.Api.Data;
using EVEMakeMoney.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EVEMakeMoney.Api.Services
{
    public class MarketPopularityService
    {
        private readonly EVEMakeMoneyDbContext _db;
        private readonly TypeNameService _typeNameService;

        public MarketPopularityService(EVEMakeMoneyDbContext db, TypeNameService typeNameService)
        {
            _db = db;
            _typeNameService = typeNameService;
        }

        public MarketPopularityResult Analyze(
            long typeId,
            long regionId,
            int recentDaysThreshold = 7,
            int clusterHours = 2)
        {
            var result = new MarketPopularityResult
            {
                TypeId = typeId,
                TypeName = _typeNameService.GetName(typeId)
            };

            var orders = _db.MarketOrders
                .Where(x => x.TypeId == typeId && x.RegionId == regionId)
                .ToList();

            var history = _db.MarketHistory
                .Where(x => x.TypeId == typeId && x.RegionId == regionId)
                .OrderByDescending(x => x.Date)
                .Take(recentDaysThreshold)
                .ToList();

            result.TotalOrders = orders.Count;

            if (orders.Count > 0)
            {
                result.TotalSupply = orders.Sum(x => x.VolumeRemain);

                var cutoffDate = DateTime.UtcNow.AddDays(-recentDaysThreshold);
                var recentOrders = orders.Where(x => x.Issued >= cutoffDate).ToList();
                result.RecentOrders = recentOrders.Count;
                result.RecentOrdersPercentage = (double)recentOrders.Count / orders.Count * 100;
                result.RecentSupply = recentOrders.Sum(x => x.VolumeRemain);
                result.RecentSupplyPercentage = result.TotalSupply > 0 
                    ? (double)result.RecentSupply / result.TotalSupply * 100 
                    : 0;

                if (recentOrders.Any())
                {
                    var hourGroups = recentOrders
                        .GroupBy(x => x.Issued.Hour)
                        .OrderByDescending(g => g.Count())
                        .Take(3)
                        .Select(g => g.Key)
                        .ToList();
                    result.HotHours = hourGroups;
                }

                var sortedOrders = recentOrders.OrderBy(x => x.Issued).ToList();
                result.TimeClusters = DetectTimeClusters(sortedOrders, clusterHours);
            }

            if (history.Any())
            {
                result.TotalVolume = history.Sum(x => x.Volume);
                result.RecentVolume = history.Sum(x => x.Volume);
                result.RecentVolumePercentage = 100;
            }

            result.PopularityScore = CalculatePopularityScore(result);

            return result;
        }

        public List<MarketPopularityResult> AnalyzeAll(
            long regionId,
            int minOrders = 5,
            int recentDaysThreshold = 7,
            int clusterHours = 2)
        {
            var typeIds = _db.MarketOrders
                .Where(x => x.RegionId == regionId)
                .GroupBy(x => x.TypeId)
                .Where(g => g.Count() >= minOrders)
                .Select(g => g.Key)
                .ToList();

            var results = new List<MarketPopularityResult>();

            foreach (var typeId in typeIds)
            {
                var result = Analyze(typeId, regionId, recentDaysThreshold, clusterHours);
                results.Add(result);
            }

            return results.OrderByDescending(x => x.PopularityScore).ToList();
        }

        private List<IssuedTimeCluster> DetectTimeClusters(List<MarketOrderEntity> orders, int clusterHours)
        {
            var clusters = new List<IssuedTimeCluster>();

            if (orders.Count == 0)
                return clusters;

            var orderedIssued = orders.Select(x => x.Issued).OrderBy(x => x).ToList();
            var currentClusterStart = orderedIssued[0];
            var currentClusterOrders = 1;

            for (int i = 1; i < orderedIssued.Count; i++)
            {
                var timeDiff = (orderedIssued[i] - orderedIssued[i - 1]).TotalHours;

                if (timeDiff <= clusterHours)
                {
                    currentClusterOrders++;
                }
                else
                {
                    var clusterEnd = orderedIssued[i - 1];
                    clusters.Add(new IssuedTimeCluster
                    {
                        StartTime = currentClusterStart,
                        EndTime = clusterEnd,
                        OrderCount = currentClusterOrders,
                        Percentage = (double)currentClusterOrders / orderedIssued.Count * 100
                    });

                    currentClusterStart = orderedIssued[i];
                    currentClusterOrders = 1;
                }
            }

            if (currentClusterOrders > 0)
            {
                clusters.Add(new IssuedTimeCluster
                {
                    StartTime = currentClusterStart,
                    EndTime = orderedIssued[^1],
                    OrderCount = currentClusterOrders,
                    Percentage = (double)currentClusterOrders / orderedIssued.Count * 100
                });
            }

            return clusters
                .OrderByDescending(x => x.OrderCount)
                .Take(5)
                .ToList();
        }

        private double CalculatePopularityScore(MarketPopularityResult result)
        {
            double score = 0;

            if (result.TotalOrders > 0)
            {
                score += Math.Min(result.TotalOrders / 10.0, 25);
            }

            if (result.TotalSupply > 0)
            {
                score += Math.Min(Math.Log10(result.TotalSupply + 1) * 3, 25);
            }

            if (result.RecentOrdersPercentage > 0)
            {
                score += result.RecentOrdersPercentage * 0.2;
            }

            if (result.HotHours.Count > 0)
            {
                score += result.HotHours.Count * 5;
            }

            var maxClusterPercentage = result.TimeClusters.Any() 
                ? result.TimeClusters.Max(x => x.Percentage) 
                : 0;
            if (maxClusterPercentage > 0)
            {
                score += maxClusterPercentage * 0.15;
            }

            return Math.Min(score, 100);
        }

        public string GenerateReport(MarketPopularityResult result)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== 市场热门指数分析 ===");
            sb.AppendLine($"物品: {result.TypeName} (ID: {result.TypeId})");
            sb.AppendLine();
            sb.AppendLine($"热门指数: {result.PopularityScore:F1}/100");
            sb.AppendLine();
            sb.AppendLine("--- 订单数据 ---");
            sb.AppendLine($"总订单数: {result.TotalOrders}");
            sb.AppendLine($"近7天订单: {result.RecentOrders} ({result.RecentOrdersPercentage:F1}%)");
            sb.AppendLine();
            sb.AppendLine("--- 供应量数据 ---");
            sb.AppendLine($"总挂单量: {result.TotalSupply:N0}");
            sb.AppendLine($"近7天挂单量: {result.RecentSupply:N0} ({result.RecentSupplyPercentage:F1}%)");
            sb.AppendLine();
            sb.AppendLine("--- 销售数据 ---");
            sb.AppendLine($"近7天销量: {result.RecentVolume:N0}");
            sb.AppendLine();

            if (result.HotHours.Any())
            {
                sb.AppendLine("--- 活跃时间段 (UTC) ---");
                sb.AppendLine(string.Join(", ", result.HotHours.Select(h => $"{h:D2}:00")));
                sb.AppendLine();
            }

            if (result.TimeClusters.Any())
            {
                sb.AppendLine("--- 集中改单时段 ---");
                foreach (var cluster in result.TimeClusters.Take(3))
                {
                    sb.AppendLine($"{cluster.StartTime:yyyy-MM-dd HH:mm} ~ {cluster.EndTime:HH:mm}");
                    sb.AppendLine($"  订单数: {cluster.OrderCount} ({cluster.Percentage:F1}%)");
                }
            }

            return sb.ToString();
        }
    }
}
