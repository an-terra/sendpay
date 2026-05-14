using SendPay.Api.DTOs.User;

namespace SendPay.Api.Services;

public interface IUserService
{
    Task<UserProfileResponse> GetProfileAsync(int userId);
    Task<UserProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest req);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest req);
    Task<ReceiverLookupResponse> LookupTransferCounterpartyAsync(
        int userId, string? phone, string? accountNumber, string? bankName);
}
