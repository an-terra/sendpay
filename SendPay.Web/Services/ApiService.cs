using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Blazored.LocalStorage;

namespace SendPay.Web.Services;

public record AuthResponse(int UserId, string Token, string FullName, string Email, string Phone, bool IsAdmin = false);
public record WalletResponse(string FullName, string Phone, decimal Balance);
public record TransactionResponse(int Id, int SenderId, string SenderName, string ReceiverName,
    decimal Amount, decimal Fee, string Note, int Type, int Status, DateTime CreatedAt);

public record AdminStatsResponse(
    int TotalUsers, int ActiveUsers,
    int TotalTransactions, int TodayTransactions,
    decimal TotalVolume, decimal TodayVolume);

public record AdminUserResponse(
    int Id, string FullName, string Email, string Phone,
    decimal Balance, bool IsActive, bool IsAdmin, DateTime CreatedAt);

public record AdminTransactionResponse(
    int Id, string SenderName, string ReceiverName,
    decimal Amount, string Note, string Type, string Status, DateTime CreatedAt);

public record RecipientResponse(int Id, string Name, string Phone, string Note, DateTime CreatedAt);
public record UserProfileResponse(int Id, string FullName, string Email, string Phone, decimal Balance, DateTime CreatedAt);
public record CurrencyRate(string Code, string Flag, string Country, decimal Rate, string Change, bool Up);
public record ExchangeRateResponse(string Date, List<CurrencyRate> Rates);

public record VerificationStartResponse(
    [property: JsonPropertyName("verificationId")] Guid VerificationId,
    [property: JsonPropertyName("expiresInSeconds")] int ExpiresInSeconds,
    [property: JsonPropertyName("debugOtp")] string? DebugOtp,
    [property: JsonPropertyName("message")] string? Message);

public class ApiService(HttpClient http, ILocalStorageService localStorage)
{
    private async Task SetAuthHeader()
    {
        var token = await localStorage.GetItemAsync<string>("token");
        http.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<(bool ok, AuthResponse? data, string error)> RegisterAsync(
        string fullName, string email, string phone, string password)
    {
        var res = await http.PostAsJsonAsync("api/auth/register",
            new { fullName, email, phone, password });

        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<AuthResponse>(), "");

        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, null, err?.Message ?? "Lỗi không xác định");
    }

    public async Task<(bool ok, AuthResponse? data, string error)> LoginAsync(
        string email, string password)
    {
        var res = await http.PostAsJsonAsync("api/auth/login", new { email, password });

        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<AuthResponse>(), "");

        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, null, err?.Message ?? "Sai email hoặc mật khẩu");
    }

    public async Task<WalletResponse?> GetWalletAsync()
    {
        await SetAuthHeader();
        return await http.GetFromJsonAsync<WalletResponse>("api/wallet");
    }

    public async Task<(bool ok, VerificationStartResponse? data, string error)> StartTopUpVerificationAsync(decimal amount)
    {
        await SetAuthHeader();
        var res = await http.PostAsJsonAsync("api/verification/topup/start", new { amount });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<VerificationStartResponse>(), "");
        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, null, err?.Message ?? "Không gửi được mã OTP");
    }

    public async Task<(bool ok, VerificationStartResponse? data, string error)> StartTransferVerificationAsync(
        string receiverPhone, decimal amount, string note)
    {
        await SetAuthHeader();
        var res = await http.PostAsJsonAsync("api/verification/transfer/start",
            new { receiverPhone, amount, note });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<VerificationStartResponse>(), "");
        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, null, err?.Message ?? "Không gửi được mã OTP");
    }

    public async Task<(bool ok, WalletResponse? data, string error)> TopUpAsync(
        decimal amount, Guid verificationId, string otpCode)
    {
        await SetAuthHeader();
        var res = await http.PostAsJsonAsync("api/wallet/topup", new { amount, verificationId, otpCode });

        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<WalletResponse>(), "");

        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, null, err?.Message ?? "Nạp tiền thất bại");
    }

    public async Task<(bool ok, TransactionResponse? data, string error)> TransferAsync(
        string receiverPhone, decimal amount, string note, Guid verificationId, string otpCode)
    {
        await SetAuthHeader();
        var res = await http.PostAsJsonAsync("api/transaction/transfer",
            new { receiverPhone, amount, note, verificationId, otpCode });

        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<TransactionResponse>(), "");

        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, null, err?.Message ?? "Chuyển tiền thất bại");
    }

    public async Task<List<TransactionResponse>> GetHistoryAsync(int page = 1)
    {
        await SetAuthHeader();
        return await http.GetFromJsonAsync<List<TransactionResponse>>(
            $"api/transaction/history?page={page}&pageSize=10") ?? [];
    }

    public async Task<TransactionResponse?> GetTransactionByIdAsync(int id)
    {
        await SetAuthHeader();
        return await http.GetFromJsonAsync<TransactionResponse>($"api/transaction/{id}");
    }

    // ── Exchange rates ─────────────────────────────────────────
    public async Task<ExchangeRateResponse?> GetRatesAsync()
    {
        await SetAuthHeader();
        return await http.GetFromJsonAsync<ExchangeRateResponse>("api/rates");
    }

    // ── Recipients ─────────────────────────────────────────────
    public async Task<List<RecipientResponse>> GetRecipientsAsync()
    {
        await SetAuthHeader();
        return await http.GetFromJsonAsync<List<RecipientResponse>>("api/recipient") ?? [];
    }

    public async Task<(bool ok, RecipientResponse? data, string error)> AddRecipientAsync(
        string name, string phone, string note)
    {
        await SetAuthHeader();
        var res = await http.PostAsJsonAsync("api/recipient", new { name, phone, note });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<RecipientResponse>(), "");
        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, null, err?.Message ?? "Thêm thất bại");
    }

    public async Task<bool> DeleteRecipientAsync(int id)
    {
        await SetAuthHeader();
        var res = await http.DeleteAsync($"api/recipient/{id}");
        return res.IsSuccessStatusCode;
    }

    // ── User profile ───────────────────────────────────────────
    public async Task<UserProfileResponse?> GetProfileAsync()
    {
        await SetAuthHeader();
        return await http.GetFromJsonAsync<UserProfileResponse>("api/user/profile");
    }

    public async Task<(bool ok, string error)> UpdateProfileAsync(
        string fullName, string email, string phone)
    {
        await SetAuthHeader();
        var res = await http.PutAsJsonAsync("api/user/profile", new { fullName, email, phone });
        if (res.IsSuccessStatusCode) return (true, "");
        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, err?.Message ?? "Cập nhật thất bại");
    }

    public async Task<(bool ok, string error)> ChangePasswordAsync(
        string currentPassword, string newPassword)
    {
        await SetAuthHeader();
        var res = await http.PutAsJsonAsync("api/user/password", new { currentPassword, newPassword });
        if (res.IsSuccessStatusCode) return (true, "");
        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, err?.Message ?? "Đổi mật khẩu thất bại");
    }

    public async Task<(bool ok, string error)> UpdateAddressAsync(
        string postalCode, string prefecture, string city, string streetAddress)
    {
        await SetAuthHeader();
        var res = await http.PutAsJsonAsync("api/user/address",
            new { postalCode, prefecture, city, streetAddress });
        if (res.IsSuccessStatusCode) return (true, "");
        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, err?.Message ?? "");
    }

    // ── Admin ──────────────────────────────────────────────────
    public async Task<AdminStatsResponse?> GetAdminStatsAsync()
    {
        await SetAuthHeader();
        return await http.GetFromJsonAsync<AdminStatsResponse>("api/admin/stats");
    }

    public async Task<List<AdminUserResponse>> GetAdminUsersAsync(int page = 1, string? search = null)
    {
        await SetAuthHeader();
        var url = $"api/admin/users?page={page}&pageSize=15";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
        return await http.GetFromJsonAsync<List<AdminUserResponse>>(url) ?? [];
    }

    public async Task<(bool ok, AdminUserResponse? data, string error)> ToggleUserAsync(int id)
    {
        await SetAuthHeader();
        var res = await http.PutAsync($"api/admin/users/{id}/toggle", null);

        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<AdminUserResponse>(), "");

        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, null, err?.Message ?? "Thao tác thất bại");
    }

    public async Task<(bool ok, AdminUserResponse? data, string error)> AdminUpdateUserAsync(
        int id, string fullName, string email, string phone, decimal? balance)
    {
        await SetAuthHeader();
        var res = await http.PutAsJsonAsync($"api/admin/users/{id}",
            new { fullName, email, phone, balance });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<AdminUserResponse>(), "");
        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, null, err?.Message ?? "Cập nhật thất bại");
    }

    public async Task<(bool ok, string error)> AdminDeleteUserAsync(int id)
    {
        await SetAuthHeader();
        var res = await http.DeleteAsync($"api/admin/users/{id}");
        if (res.IsSuccessStatusCode) return (true, "");
        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, err?.Message ?? "Xóa thất bại");
    }

    public async Task<List<AdminTransactionResponse>> GetAdminTransactionsAsync(int page = 1)
    {
        await SetAuthHeader();
        return await http.GetFromJsonAsync<List<AdminTransactionResponse>>(
            $"api/admin/transactions?page={page}&pageSize=15") ?? [];
    }

    private record ErrorResponse(string Message);
}
