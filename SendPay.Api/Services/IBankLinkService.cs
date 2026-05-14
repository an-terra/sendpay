using SendPay.Api.DTOs.BankLink;

namespace SendPay.Api.Services;

public interface IBankLinkService
{
    Task<BankLinkStartResponse> StartAsync(int userId, BankLinkStartRequest req, string? ipAddress, CancellationToken ct = default);
    Task<FakeBankApproveResponse> FakeApproveAsync(FakeBankApproveRequest req, string? ipAddress, CancellationToken ct = default);
    Task<List<UserBankLinkDto>> GetMyLinksAsync(int userId, CancellationToken ct = default);
    Task<UserBankLinkDto?> GetPrimaryAsync(int userId, CancellationToken ct = default);
    Task<bool> UnlinkAsync(int userId, int linkId, CancellationToken ct = default);
    Task<bool> SetPrimaryAsync(int userId, int linkId, CancellationToken ct = default);
}
