namespace SendPay.Api.Services;

public interface IReconciliationService
{
    /// <returns>Số lệnh nạp được khớp.</returns>
    Task<int> MatchBankCreditsAsync(CancellationToken ct = default);

    Task<int> ExpireStaleTopUpIntentsAsync(CancellationToken ct = default);

    /// <summary>Ghi snapshot thống kê cho một ngày UTC (00:00 của ngày đó).</summary>
    Task RebuildDailyStatsForUtcDateAsync(DateTime utcDayStart, CancellationToken ct = default);

    Task<(bool ok, string error)> AdminConfirmTopUpAsync(int intentId, CancellationToken ct = default);
}
