namespace SendPay.Api.DTOs.Admin;

public class AdminStatsResponse
{
    public int     TotalUsers         { get; init; }
    public int     ActiveUsers        { get; init; }
    public int     TotalTransactions  { get; init; }
    public decimal TotalVolume        { get; init; }
    public int     TodayTransactions  { get; init; }
    public decimal TodayVolume        { get; init; }
}
