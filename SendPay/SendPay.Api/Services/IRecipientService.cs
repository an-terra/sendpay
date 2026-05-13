using SendPay.Api.DTOs.Recipient;

namespace SendPay.Api.Services;

public interface IRecipientService
{
    Task<List<RecipientResponse>> GetAllAsync(int userId);
    Task<RecipientResponse> AddAsync(int userId, RecipientRequest req);
    Task DeleteAsync(int userId, int id);
}
