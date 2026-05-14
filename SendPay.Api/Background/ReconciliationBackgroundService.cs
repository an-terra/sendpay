using SendPay.Api.Services;

namespace SendPay.Api.Background;

public class ReconciliationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<ReconciliationBackgroundService> logger) : BackgroundService
{
    DateTime? _lastDailyStatsDayComputed;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var matchMinutes = config.GetValue("Reconciliation:MatchIntervalMinutes", 15);
        var statsHour = config.GetValue("Reconciliation:DailyStatsUtcHour", 1);

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var recon = scope.ServiceProvider.GetRequiredService<IReconciliationService>();

                var matched = await recon.MatchBankCreditsAsync(stoppingToken);
                var expired = await recon.ExpireStaleTopUpIntentsAsync(stoppingToken);
                if (matched > 0 || expired > 0)
                    logger.LogInformation("Đối soát: khớp {Matched} ghi có, hết hạn {Expired} lệnh nạp.", matched, expired);

                await MaybeRunDailyStatsAsync(recon, statsHour, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi job đối soát / thống kê.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(matchMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    async Task MaybeRunDailyStatsAsync(IReconciliationService recon, int statsHourUtc, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (now.Hour < statsHourUtc)
            return;

        var dayMarker = now.Date;
        if (_lastDailyStatsDayComputed == dayMarker)
            return;

        var yesterday = dayMarker.AddDays(-1);
        await recon.RebuildDailyStatsForUtcDateAsync(yesterday, ct);
        _lastDailyStatsDayComputed = dayMarker;
        logger.LogInformation("Đã cập nhật DailyTransactionStats cho UTC {Day:yyyy-MM-dd}.", yesterday);
    }
}
