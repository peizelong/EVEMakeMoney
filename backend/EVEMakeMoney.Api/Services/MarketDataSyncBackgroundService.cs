using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EVEMakeMoney.Api.Services
{
    public class MarketDataSyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MarketDataSyncBackgroundService> _logger;
        private readonly MarketDataSyncOptions _options;
        private DateTime _lastOrderSync = DateTime.MinValue;
        private DateTime _lastHistorySync = DateTime.MinValue;

        public MarketDataSyncBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<MarketDataSyncBackgroundService> logger,
            IOptions<MarketDataSyncOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Market Data Sync Service started");

            if (!_options.Enabled)
            {
                _logger.LogInformation("Market Data Sync Service is disabled");
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;

                    if (now - _lastOrderSync >= TimeSpan.FromMinutes(_options.OrderSyncIntervalMinutes))
                    {
                        _logger.LogInformation("Starting scheduled market order sync for region {RegionId}", _options.DefaultRegionId);
                        await SyncMarketOrdersAsync(stoppingToken);
                        _lastOrderSync = now;
                        _logger.LogInformation("Market order sync completed. Next sync at {NextSync}", _lastOrderSync.AddMinutes(_options.OrderSyncIntervalMinutes));
                    }

                    if (now - _lastHistorySync >= TimeSpan.FromHours(_options.HistorySyncIntervalHours))
                    {
                        _logger.LogInformation("Starting scheduled market history sync for region {RegionId}", _options.DefaultRegionId);
                        await SyncMarketHistoryAsync(stoppingToken);
                        _lastHistorySync = now;
                        _logger.LogInformation("Market history sync completed. Next sync at {NextSync}", _lastHistorySync.AddHours(_options.HistorySyncIntervalHours));
                    }

                    var nextOrderSync = _lastOrderSync.AddMinutes(_options.OrderSyncIntervalMinutes) - now;
                    var nextHistorySync = _lastHistorySync.AddHours(_options.HistorySyncIntervalHours) - now;
                    var nextSync = TimeSpan.FromSeconds(60);

                    if (nextOrderSync > TimeSpan.Zero && nextOrderSync < nextSync)
                    {
                        nextSync = nextOrderSync;
                    }
                    if (nextHistorySync > TimeSpan.Zero && nextHistorySync < nextSync)
                    {
                        nextSync = nextHistorySync;
                    }

                    if (nextSync < TimeSpan.FromSeconds(60))
                    {
                        nextSync = TimeSpan.FromSeconds(60);
                    }

                    await Task.Delay(nextSync, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in market data sync service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Market Data Sync Service stopped");
        }

        private async Task SyncMarketOrdersAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var marketDataService = scope.ServiceProvider.GetRequiredService<MarketDataService>();

            try
            {
                var progress = new Progress<MarketFetchProgress>(p =>
                {
                    if (p.Phase == "Complete")
                    {
                        _logger.LogInformation("Fetched {Count} market orders", p.OrdersFetched);
                    }
                });

                await marketDataService.FetchMarketOrdersAsync(_options.DefaultRegionId, progress, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync market orders");
            }
        }

        private async Task SyncMarketHistoryAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var marketDataService = scope.ServiceProvider.GetRequiredService<MarketDataService>();
            var blueprintCache = scope.ServiceProvider.GetRequiredService<BlueprintCacheService>();

            try
            {
                var blueprints = blueprintCache.GetBlueprints();
                var typeIds = new HashSet<long>();

                foreach (var bp in blueprints)
                {
                    if (bp.Activities?.Manufacturing?.Materials != null)
                    {
                        foreach (var material in bp.Activities.Manufacturing.Materials)
                        {
                            typeIds.Add(material.TypeId);
                        }
                    }

                    if (bp.Activities?.Invention?.Materials != null)
                    {
                        foreach (var material in bp.Activities.Invention.Materials)
                        {
                            typeIds.Add(material.TypeId);
                        }
                    }

                    if (bp.Activities?.Reaction?.Materials != null)
                    {
                        foreach (var material in bp.Activities.Reaction.Materials)
                        {
                            typeIds.Add(material.TypeId);
                        }
                    }
                }

                _logger.LogInformation("Syncing market history for {Count} types", typeIds.Count);

                var progress = new Progress<int>(p =>
                {
                    if (p % 10 == 0)
                    {
                        _logger.LogInformation("Market history sync progress: {Progress}%", p);
                    }
                });

                await marketDataService.FetchAllMarketHistoryAsync(_options.DefaultRegionId, typeIds.ToList(), progress, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync market history");
            }
        }

        public void TriggerSync(bool orders = true, bool history = false)
        {
            if (orders)
            {
                _lastOrderSync = DateTime.MinValue;
            }
            if (history)
            {
                _lastHistorySync = DateTime.MinValue;
            }
        }
    }
}
