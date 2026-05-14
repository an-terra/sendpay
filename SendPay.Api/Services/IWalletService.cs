using SendPay.Api.DTOs.Wallet;

namespace SendPay.Api.Services;

public interface IWalletService
{
    Task<WalletResponse> GetBalanceAsync(int userId);
    Task<WalletTopUpResponse> TopUpAsync(int userId, TopUpRequest request);
}
