using SendPay.Api.DTOs.Recipient;

namespace SendPay.Api.Services;

public interface IRecipientService
{
    Task<List<RecipientResponse>> GetAllAsync(int userId);
    Task<RecipientResponse> GetByIdAsync(int userId, int id);
    Task<RecipientResponse> AddAsync(int userId, RecipientRequest req);
    Task<RecipientResponse> UpdateAsync(int userId, int id, RecipientRequest req);
    Task DeleteAsync(int userId, int id);
}
