using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EVEStandard;
using EVEStandard.Models;
using Microsoft.EntityFrameworkCore;
using QuickType;

namespace EVEMakeMoney.Data
{
    public class MarketDataService
    {
        private readonly EVEStandardAPI _esi;
        private readonly HttpClient _httpClient;

        public MarketDataService(EVEStandardAPI esi)
        {
            _esi = esi;
            _httpClient = new HttpClient();
        }

        public async Task FetchAndSaveMarketOrdersAsync(long regionId)
        {
            var result = await _esi.Market.ListOrdersInRegionAsync(regionId);
            var orders = result.Model;

            using var db = new EVEMakeMoneyDbContext();
            var fetchedAt = DateTime.UtcNow;

            var existingOrderIds = await db.MarketOrders
                .Where(x => x.RegionId == regionId)
                .Select(x => x.OrderId)
                .ToListAsync();

            var newOrders = new List<MarketOrderEntity>();
            var updatedOrders = new List<MarketOrderEntity>();

            foreach (var order in orders)
            {
                var entity = new MarketOrderEntity
                {
                    OrderId = order.OrderId,
                    TypeId = order.TypeId,
                    RegionId = regionId,
                    SystemId = order.SystemId,
                    LocationId = order.LocationId,
                    IsBuyOrder = order.IsBuyOrder,
                    Price = order.Price,
                    VolumeRemain = order.VolumeRemain,
                    VolumeTotal = order.VolumeTotal,
                    Duration = order.Duration,
                    MinVolume = order.MinVolume,
                    Range = order.Range,
                    Issued = order.Issued,
                    FetchedAt = fetchedAt
                };

                if (existingOrderIds.Contains(order.OrderId))
                {
                    updatedOrders.Add(entity);
                }
                else
                {
                    newOrders.Add(entity);
                }
            }

            if (newOrders.Any())
            {
                await db.MarketOrders.AddRangeAsync(newOrders);
            }

            if (updatedOrders.Any())
            {
                db.MarketOrders.UpdateRange(updatedOrders);
            }

            await db.SaveChangesAsync();
        }

        public async Task<List<long>> GetTypeIdsFromOrdersAsync(long regionId)
        {
            int page = 1;
            var typeIds = new HashSet<long>();
            var fetchedAt = DateTime.UtcNow;

            using var db = new EVEMakeMoneyDbContext();

            // 先清空该区域的订单
            var existingOrders = await db.MarketOrders
                .Where(x => x.RegionId == regionId)
                .ToListAsync();
            db.MarketOrders.RemoveRange(existingOrders);
            await db.SaveChangesAsync();

            while (true)
            {
                try
                {
                    var result = await _esi.Market.ListOrdersInRegionAsync(regionId, page: page);
                    
                    if (result.Model == null || !result.Model.Any())
                        break;

                    var newOrders = new List<MarketOrderEntity>();

                    foreach (var order in result.Model)
                    {
                        typeIds.Add(order.TypeId);

                        var entity = new MarketOrderEntity
                        {
                            OrderId = order.OrderId,
                            TypeId = order.TypeId,
                            RegionId = regionId,
                            SystemId = order.SystemId,
                            LocationId = order.LocationId,
                            IsBuyOrder = order.IsBuyOrder,
                            Price = order.Price,
                            VolumeRemain = order.VolumeRemain,
                            VolumeTotal = order.VolumeTotal,
                            Duration = order.Duration,
                            MinVolume = order.MinVolume,
                            Range = order.Range,
                            Issued = order.Issued,
                            FetchedAt = fetchedAt
                        };

                        newOrders.Add(entity);
                    }

                    if (newOrders.Any())
                    {
                        await db.MarketOrders.AddRangeAsync(newOrders);
                        await db.SaveChangesAsync();
                    }

                    if (result.Model.Count < 1000)
                        break;

                    page++;
                }
                catch (Exception)
                {
                    break;
                }
            }

            return typeIds.ToList();
        }

        public async Task FetchAndSaveMarketHistoryAsync(long regionId, long typeId)
        {
            var result = await _esi.Market.ListHistoricalMarketStatisticsInRegionAsync(regionId, typeId);
            var history = result.Model;

            using var db = new EVEMakeMoneyDbContext();
            var fetchedAt = DateTime.UtcNow;

            foreach (var entry in history)
            {
                var existing = await db.MarketHistory
                    .FirstOrDefaultAsync(x => x.TypeId == typeId && x.RegionId == regionId && x.Date == entry.Date);

                if (existing == null)
                {
                    await db.MarketHistory.AddAsync(new MarketHistoryEntity
                    {
                        TypeId = typeId,
                        RegionId = regionId,
                        Date = entry.Date,
                        Average = entry.Average,
                        Highest = entry.Highest,
                        Lowest = entry.Lowest,
                        OrderCount = entry.OrderCount,
                        Volume = entry.Volume,
                        FetchedAt = fetchedAt
                    });
                }
            }

            await db.SaveChangesAsync();
        }

        public async Task CalculateWeeklySalesAsync(long regionId, long typeId)
        {
            using var db = new EVEMakeMoneyDbContext();
            var history = await db.MarketHistory
                .Where(x => x.TypeId == typeId && x.RegionId == regionId)
                .OrderBy(x => x.Date)
                .ToListAsync();

            if (!history.Any())
                return;

            var weeklyGroups = history.GroupBy(x => GetWeekNumber(x.Date));

            foreach (var group in weeklyGroups)
            {
                var weekData = group.ToList();
                var firstDate = weekData.Min(x => x.Date);
                var lastDate = weekData.Max(x => x.Date);
                var year = firstDate.Year;
                var weekNumber = GetIso8601WeekOfYear(firstDate);

                var existing = await db.WeeklySales
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.TypeId == typeId && x.RegionId == regionId && x.Year == year && x.WeekNumber == weekNumber);

                var weeklySales = new WeeklySalesEntity
                {
                    TypeId = typeId,
                    RegionId = regionId,
                    Year = year,
                    WeekNumber = weekNumber,
                    TotalVolume = weekData.Sum(x => x.Volume),
                    AveragePrice = weekData.Average(x => x.Average),
                    HighestPrice = weekData.Max(x => x.Highest),
                    LowestPrice = weekData.Min(x => x.Lowest),
                    OrderCount = weekData.Sum(x => x.OrderCount),
                    WeekStart = firstDate,
                    WeekEnd = lastDate,
                    UpdatedAt = DateTime.UtcNow
                };

                if (existing != null)
                {
                    weeklySales.Id = existing.Id;
                    db.WeeklySales.Update(weeklySales);
                }
                else
                {
                    await db.WeeklySales.AddAsync(weeklySales);
                }
            }

            await db.SaveChangesAsync();
        }

        public List<long> GetTypeIdsFromBlueprints(string blueprintsFilePath)
        {
            var typeIds = new HashSet<long>();

            foreach (var line in File.ReadLines(blueprintsFilePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var blueprint = QuickType.Blueprint.FromJson(line);

                if (blueprint.Activities?.Manufacturing?.Materials != null)
                {
                    foreach (var material in blueprint.Activities.Manufacturing.Materials)
                    {
                        typeIds.Add(material.TypeId);
                    }
                }

                if (blueprint.Activities?.Invention?.Materials != null)
                {
                    foreach (var material in blueprint.Activities.Invention.Materials)
                    {
                        typeIds.Add(material.TypeId);
                    }
                }

                if (blueprint.Activities?.Invention?.Products != null)
                {
                    foreach (var product in blueprint.Activities.Invention.Products)
                    {
                        typeIds.Add(product.TypeId);
                    }
                }
            }

            return typeIds.ToList();
        }

        public async Task FetchAndSaveAllMarketHistoryAsync(long regionId, List<long> typeIds, IProgress<int>? progress = null)
        {
            int total = typeIds.Count;
            int completed = 0;
            int maxConcurrency = 10;
            using var semaphore = new SemaphoreSlim(maxConcurrency);

            var tasks = typeIds.Select(async typeId =>
            {
                await semaphore.WaitAsync();
                try
                {
                    try
                    {
                        await FetchAndSaveMarketHistoryAsync(regionId, typeId);
                    }
                    catch
                    {
                    }

                    var current = Interlocked.Increment(ref completed);
                    progress?.Report((int)((double)current / total * 100));
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        public async Task CalculateAllWeeklySalesAsync(long regionId, IProgress<int>? progress = null)
        {
            using var db = new EVEMakeMoneyDbContext();
            var typeIds = await db.MarketHistory
                .Where(x => x.RegionId == regionId)
                .Select(x => x.TypeId)
                .Distinct()
                .ToListAsync();

            int total = typeIds.Count;
            int completed = 0;

            foreach (var typeId in typeIds)
            {
                await CalculateWeeklySalesAsync(regionId, typeId);
                completed++;
                progress?.Report((int)((double)completed / total * 100));
            }
        }

        private string GetWeekNumber(DateTime date)
        {
            var week = GetIso8601WeekOfYear(date);
            return $"{date.Year}-W{week:D2}";
        }

        private int GetIso8601WeekOfYear(DateTime time)
        {
            var day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}
