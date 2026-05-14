namespace SendPay.Api.Services.BankLink;

/// <summary>
/// Tạo URL / route chuyển hướng OAuth-style. Demo: Fake.
/// Ký hợp đồng NH Nhật: triển khai connector trả URL sang provider thật và đặt BankLink:Provider trong cấu hình.
/// </summary>
public interface IBankLinkAuthorizePathBuilder
{
    string BuildAuthorizeUrl(string bankCode, string state, string? returnUrl);
}
