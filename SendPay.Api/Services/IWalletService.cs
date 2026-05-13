using SendPay.Api.DTOs.Wallet;

namespace SendPay.Api.Services;

public interface IWalletService
{
    Task<WalletResponse> GetBalanceAsync(int userId);
    Task<WalletResponse> TopUpAsync(int userId, TopUpRequest request);
}
