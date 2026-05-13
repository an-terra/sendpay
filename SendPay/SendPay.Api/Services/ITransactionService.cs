using SendPay.Api.DTOs.Transaction;

namespace SendPay.Api.Services;

public interface ITransactionService
{
    Task<TransactionResponse>       TransferAsync(int senderId, TransferRequest request);
    Task<TransactionResponse>       GetByIdAsync(int userId, int id);
    Task<List<TransactionResponse>> GetHistoryAsync(int userId, int page, int pageSize);
}
