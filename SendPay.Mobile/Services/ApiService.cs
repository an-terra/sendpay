using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Maui.Devices;

namespace SendPay.Mobile.Services;

public record AuthResponse(string Token, string FullName, string Email, string Phone);
public record WalletResponse(string FullName, string Phone, decimal Balance);
public record TransactionResponse(int Id, string SenderName, string ReceiverName,
    decimal Amount, string Note, int Type, int Status, DateTime CreatedAt);

public record VerificationStartResponse(
    [property: JsonPropertyName("verificationId")] Guid VerificationId,
    [property: JsonPropertyName("expiresInSeconds")] int ExpiresInSeconds,
    [property: JsonPropertyName("debugOtp")] string? DebugOtp,
    [property: JsonPropertyName("message")] string? Message);

public record ReceiverLookupDto(
    [property: JsonPropertyName("found")] bool Found,
    [property: JsonPropertyName("fullName")] string? FullName,
    [property: JsonPropertyName("isSelf")] bool IsSelf,
    [property: JsonPropertyName("savedRecipientId")] int? SavedRecipientId,
    [property: JsonPropertyName("matchKind")] string? MatchKind,
    [property: JsonPropertyName("bankDisplay")] string? BankDisplay,
    [property: JsonPropertyName("resolvedPhone")] string? ResolvedPhone);

public record RecipientResponse(
    int Id, string Name, string? Phone, string Note,
    string? BankName, string? AccountNumber, string? AccountHolderName, DateTime CreatedAt);

public class ApiService
{
    private readonly HttpClient _http;

    private static string ResolveApiBaseUrl()
    {
        if (DeviceInfo.Platform == DevicePlatform.Android)
            return "http://10.0.2.2:5050/";
        return "http://localhost:5050/";
    }

    public ApiService()
    {
        _http = new HttpClient { BaseAddress = new Uri(ResolveApiBaseUrl()) };
    }

    private void SetToken()
    {
        var token = Preferences.Get("token", "");
        _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<(bool ok, AuthResponse? data, string error)> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<AuthResponse>(), "");
        return (false, null, "Sai email hoặc mật khẩu");
    }

    public async Task<(bool ok, AuthResponse? data, string error)> RegisterAsync(
        string fullName, string email, string phone, string password)
    {
        var res = await _http.PostAsJsonAsync("api/auth/register", new { fullName, email, phone, password });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<AuthResponse>(), "");
        return (false, null, "Đăng ký thất bại. Email hoặc SĐT đã tồn tại.");
    }

    public async Task<WalletResponse?> GetWalletAsync()
    {
        SetToken();
        return await _http.GetFromJsonAsync<WalletResponse>("api/wallet");
    }

    public async Task<(bool ok, VerificationStartResponse? data, string error)> StartTopUpVerificationAsync(decimal amount)
    {
        SetToken();
        var res = await _http.PostAsJsonAsync("api/verification/topup/start", new { amount });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<VerificationStartResponse>(), "");
        return (false, null, "Không gửi được OTP");
    }

    public async Task<(bool ok, VerificationStartResponse? data, string error)> StartTransferVerificationAsync(
        string receiverPhone, decimal amount, string note)
    {
        SetToken();
        var res = await _http.PostAsJsonAsync("api/verification/transfer/start",
            new { receiverPhone, amount, note });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<VerificationStartResponse>(), "");
        return (false, null, "Không gửi được OTP");
    }

    public async Task<(bool ok, WalletResponse? data, string error)> TopUpAsync(
        decimal amount, Guid verificationId, string otpCode)
    {
        SetToken();
        var res = await _http.PostAsJsonAsync("api/wallet/topup", new { amount, verificationId, otpCode });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<WalletResponse>(), "");
        return (false, null, "Nạp tiền thất bại");
    }

    public async Task<ReceiverLookupDto?> LookupTransferCounterpartyAsync(
        string? phone, string? accountNumber = null, string? bankName = null)
    {
        SetToken();
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(phone))
            parts.Add($"phone={Uri.EscapeDataString(phone.Trim())}");
        if (!string.IsNullOrWhiteSpace(accountNumber))
            parts.Add($"accountNumber={Uri.EscapeDataString(accountNumber.Trim())}");
        if (!string.IsNullOrWhiteSpace(bankName))
            parts.Add($"bankName={Uri.EscapeDataString(bankName.Trim())}");
        if (parts.Count == 0) return null;
        return await _http.GetFromJsonAsync<ReceiverLookupDto>($"api/user/receiver-lookup?{string.Join("&", parts)}");
    }

    public async Task<RecipientResponse?> GetRecipientByIdAsync(int id)
    {
        SetToken();
        var res = await _http.GetAsync($"api/recipient/{id}");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<RecipientResponse>();
    }

    public async Task<List<RecipientResponse>> GetRecipientsAsync()
    {
        SetToken();
        return await _http.GetFromJsonAsync<List<RecipientResponse>>("api/recipient") ?? [];
    }

    public async Task<(bool ok, RecipientResponse? data, string error)> AddRecipientAsync(
        string name, string? phone, string note,
        string? bankName = null, string? accountNumber = null, string? accountHolderName = null)
    {
        SetToken();
        var res = await _http.PostAsJsonAsync("api/recipient",
            new { name, phone, note, bankName, accountNumber, accountHolderName });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<RecipientResponse>(), "");
        var err = await res.Content.ReadAsStringAsync();
        return (false, null, err.Length > 200 ? "Thêm thất bại" : err);
    }

    public async Task<bool> DeleteRecipientAsync(int id)
    {
        SetToken();
        var res = await _http.DeleteAsync($"api/recipient/{id}");
        return res.IsSuccessStatusCode;
    }

    public async Task<(bool ok, TransactionResponse? data, string error)> TransferAsync(
        string receiverPhone, decimal amount, string note, Guid verificationId, string otpCode)
    {
        SetToken();
        var res = await _http.PostAsJsonAsync("api/transaction/transfer",
            new { receiverPhone, amount, note, verificationId, otpCode });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<TransactionResponse>(), "");
        return (false, null, "Chuyển tiền thất bại. Kiểm tra số dư, OTP hoặc SĐT.");
    }

    public async Task<List<TransactionResponse>> GetHistoryAsync(int page = 1)
    {
        SetToken();
        return await _http.GetFromJsonAsync<List<TransactionResponse>>(
            $"api/transaction/history?page={page}&pageSize=20") ?? [];
    }
}
