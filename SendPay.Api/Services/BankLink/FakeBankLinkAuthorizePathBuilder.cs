namespace SendPay.Api.Services.BankLink;

public sealed class FakeBankLinkAuthorizePathBuilder(IConfiguration configuration, ILogger<FakeBankLinkAuthorizePathBuilder> logger)
    : IBankLinkAuthorizePathBuilder
{
    public string BuildAuthorizeUrl(string bankCode, string state, string? returnUrl)
    {
        var provider = (configuration["BankLink:Provider"] ?? "Fake").Trim();
        if (!provider.Equals("Fake", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("BankLink:Provider={Provider} chưa được triển khai. Dùng Fake cho đến khi tích hợp đối tác.", provider);
            throw new InvalidOperationException(
                $"Provider '{provider}' chưa kết nối. Đặt BankLink:Provider=Fake hoặc hoàn tất tích hợp Open Banking.");
        }

        return $"/fake-bank/{Uri.EscapeDataString(bankCode)}?state={Uri.EscapeDataString(state)}";
    }
}
