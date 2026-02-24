namespace EVEMakeMoney.Api.DTOs.Market;

public class MarketDataStatus
{
    public bool IsRunning { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public int TotalOrders { get; set; }
    public int TotalHistory { get; set; }
    public string? CurrentTask { get; set; }
    public double Progress { get; set; }
}
