namespace EVEMakeMoney.Api.Services
{
    public class MarketDataSyncOptions
    {
        public bool Enabled { get; set; } = true;
        public int OrderSyncIntervalMinutes { get; set; } = 60;
        public int HistorySyncIntervalHours { get; set; } = 24;
        public long DefaultRegionId { get; set; } = 10000002;
        public int HistoryBatchSize { get; set; } = 100;
        public int MaxConcurrency { get; set; } = 10;
    }
}
