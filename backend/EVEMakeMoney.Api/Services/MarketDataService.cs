using System.Collections.Concurrent;
using EVEMakeMoney.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EVEMakeMoney.Api.Services
{
    public class MarketDataService
    {
        private readonly EVEMakeMoneyDbContext _db;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MarketDataService> _logger;
        private const string ESI_BASE_URL = "https://esi.evetech.net/latest";

        public MarketDataService(EVEMakeMoneyDbContext db, ILogger<MarketDataService> logger)
        {
            _db = db;
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "EVEMakeMoney/1.0");
        }

        public async Task FetchMarketOrdersAsync(long regionId, IProgress<MarketFetchProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            var fetchedAt = DateTime.UtcNow;
            var allOrders = new ConcurrentBag<MarketOrderEntity>();
            int page = 1;
            int totalPages = 1;
            int processedPages = 0;

            var existingOrderIds = await _db.MarketOrders
                .Where(x => x.RegionId == regionId)
                .Select(x => x.OrderId)
                .ToListAsync(cancellationToken);

            var existingSet = new HashSet<long>(existingOrderIds);

            while (page <= totalPages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var response = await _httpClient.GetAsync(
                        $"{ESI_BASE_URL}/markets/{regionId}/orders/?datasource=tranquility&page={page}",
                        cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Failed to fetch page {Page}: {StatusCode}", page, response.StatusCode);
                        break;
                    }

                    if (response.Headers.TryGetValues("x-pages", out var pagesHeader))
                    {
                        if (int.TryParse(pagesHeader.FirstOrDefault(), out var pages))
                        {
                            totalPages = pages;
                        }
                    }

                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var orders = System.Text.Json.JsonSerializer.Deserialize<List<ESIMarketOrder>>(content);

                    if (orders == null || orders.Count == 0)
                    {
                        break;
                    }

                    foreach (var order in orders)
                    {
                        allOrders.Add(new MarketOrderEntity
                        {
                            OrderId = order.order_id,
                            TypeId = order.type_id,
                            RegionId = regionId,
                            SystemId = order.system_id,
                            LocationId = order.location_id,
                            IsBuyOrder = order.is_buy_order,
                            Price = order.price,
                            VolumeRemain = order.volume_remain,
                            VolumeTotal = order.volume_total,
                            Duration = order.duration,
                            MinVolume = order.min_volume,
                            Range = order.range,
                            Issued = order.issued,
                            FetchedAt = fetchedAt
                        });
                    }

                    processedPages++;
                    progress?.Report(new MarketFetchProgress
                    {
                        CurrentPage = page,
                        TotalPages = totalPages,
                        OrdersFetched = allOrders.Count,
                        Phase = "Fetching orders"
                    });

                    page++;

                    await Task.Delay(100, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching page {Page}", page);
                    break;
                }
            }

            progress?.Report(new MarketFetchProgress
            {
                CurrentPage = totalPages,
                TotalPages = totalPages,
                OrdersFetched = allOrders.Count,
                Phase = "Saving to database"
            });

            var ordersToDelete = await _db.MarketOrders
                .Where(x => x.RegionId == regionId)
                .ToListAsync(cancellationToken);
            _db.MarketOrders.RemoveRange(ordersToDelete);

            var ordersToSave = allOrders.ToList();
            const int batchSize = 1000;
            for (int i = 0; i < ordersToSave.Count; i += batchSize)
            {
                var batch = ordersToSave.Skip(i).Take(batchSize);
                await _db.MarketOrders.AddRangeAsync(batch, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

            progress?.Report(new MarketFetchProgress
            {
                CurrentPage = totalPages,
                TotalPages = totalPages,
                OrdersFetched = allOrders.Count,
                Phase = "Complete"
            });
        }

        public async Task FetchMarketHistoryAsync(long regionId, long typeId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{ESI_BASE_URL}/markets/{regionId}/history/?datasource=tranquility&type_id={typeId}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var history = System.Text.Json.JsonSerializer.Deserialize<List<ESIMarketHistory>>(content);

                if (history == null || history.Count == 0)
                {
                    return;
                }

                var fetchedAt = DateTime.UtcNow;

                foreach (var entry in history)
                {
                    var existing = await _db.MarketHistory
                        .FirstOrDefaultAsync(x => x.TypeId == typeId && x.RegionId == regionId && x.Date == entry.date, cancellationToken);

                    if (existing == null)
                    {
                        _db.MarketHistory.Add(new MarketHistoryEntity
                        {
                            TypeId = typeId,
                            RegionId = regionId,
                            Date = entry.date,
                            Average = entry.average,
                            Highest = entry.highest,
                            Lowest = entry.lowest,
                            OrderCount = entry.order_count,
                            Volume = entry.volume,
                            FetchedAt = fetchedAt
                        });
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching history for type {TypeId}", typeId);
            }
        }

        public async Task FetchAllMarketHistoryAsync(long regionId, List<long> typeIds, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            int total = typeIds.Count;
            int completed = 0;
            int maxConcurrency = 10;
            using var semaphore = new SemaphoreSlim(maxConcurrency);

            var tasks = typeIds.Select(async typeId =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    await FetchMarketHistoryAsync(regionId, typeId, cancellationToken);
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

        public async Task CalculateWeeklySalesAsync(long regionId, long typeId)
        {
            var history = await _db.MarketHistory
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

                var existing = await _db.WeeklySales
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
                    _db.WeeklySales.Update(weeklySales);
                }
                else
                {
                    await _db.WeeklySales.AddAsync(weeklySales);
                }
            }

            await _db.SaveChangesAsync();
        }

        private string GetWeekNumber(DateTime date)
        {
            var week = GetIso8601WeekOfYear(date);
            return $"{date.Year}-W{week:D2}";
        }

        private int GetIso8601WeekOfYear(DateTime time)
        {
            var day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }
            return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public async Task<Dictionary<long, double>> GetMarketPricesAsync()
        {
            var prices = new Dictionary<long, double>();

            var buyOrders = await _db.MarketOrders
                .Where(x => x.IsBuyOrder == true)
                .GroupBy(x => x.TypeId)
                .Select(g => new
                {
                    TypeId = g.Key,
                    MaxPrice = g.Max(x => x.Price)
                })
                .ToListAsync();

            foreach (var order in buyOrders)
            {
                prices[order.TypeId] = order.MaxPrice;
            }

            return prices;
        }
    }

    public class MarketFetchProgress
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int OrdersFetched { get; set; }
        public string Phase { get; set; } = "";
    }

    public class ESIMarketOrder
    {
        public long order_id { get; set; }
        public long type_id { get; set; }
        public long system_id { get; set; }
        public long location_id { get; set; }
        public bool is_buy_order { get; set; }
        public double price { get; set; }
        public int volume_remain { get; set; }
        public int volume_total { get; set; }
        public int duration { get; set; }
        public int min_volume { get; set; }
        public string range { get; set; } = "";
        public DateTime issued { get; set; }
    }

    public class ESIMarketHistory
    {
        public DateTime date { get; set; }
        public double average { get; set; }
        public double highest { get; set; }
        public double lowest { get; set; }
        public long order_count { get; set; }
        public long volume { get; set; }
    }
}
