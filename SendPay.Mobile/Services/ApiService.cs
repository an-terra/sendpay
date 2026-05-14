using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Maui.Devices;

namespace SendPay.Mobile.Services;

public record AuthResponse(string Token, string FullName, string Email, string Phone);
public record WalletResponse(string FullName, string Phone, decimal Balance);

public class WalletTopUpResponseDto
{
    public string Mode { get; set; } = "";
    public WalletResponse? Wallet { get; set; }
    public int? IntentId { get; set; }
    public string? ReferenceCode { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public decimal? ExpectedAmount { get; set; }
}

public class UserProfileDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? JapanBankName { get; set; }
    public string? JapanBankTopUpUrl { get; set; }
}

public class TransactionResponse
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = "";
    public string ReceiverName { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public string Note { get; set; } = "";
    public int Type { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ReceiverBankName { get; set; }
    public string? ReceiverAccountNumber { get; set; }
}

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
    string? CountryCode,
    string? BankName, string? AccountNumber, string? AccountHolderName, string? SwiftBic, DateTime CreatedAt);

public record CatalogBankOption(string Name, string Swift);

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

    public async Task<UserProfileDto?> GetProfileAsync()
    {
        SetToken();
        return await _http.GetFromJsonAsync<UserProfileDto>("api/user/profile");
    }

    public async Task<(bool ok, string error)> UpdateProfileAsync(
        string fullName, string email, string phone,
        string? japanBankName, string? japanBankTopUpUrl)
    {
        SetToken();
        var res = await _http.PutAsJsonAsync("api/user/profile",
            new { fullName, email, phone, japanBankName, japanBankTopUpUrl });
        if (res.IsSuccessStatusCode) return (true, "");
        var body = await res.Content.ReadAsStringAsync();
        return (false, body.Length > 240 ? "Cập nhật thất bại" : body);
    }

    public async Task<(bool ok, WalletTopUpResponseDto? data, string error)> TopUpAsync(decimal amount)
    {
        SetToken();
        var res = await _http.PostAsJsonAsync("api/wallet/topup", new { amount });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<WalletTopUpResponseDto>(), "");
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

    public async Task<List<CatalogBankOption>> GetCatalogBanksAsync(string countryCode)
    {
        SetToken();
        var c = (countryCode ?? "").Trim().ToUpperInvariant();
        var raw = await _http.GetFromJsonAsync<List<BankCatalogJsonDto>>(
            $"api/reference/banks/{Uri.EscapeDataString(c)}") ?? [];
        return raw.Select(x => new CatalogBankOption(x.name, x.swift)).ToList();
    }

    public Task<List<CatalogBankOption>> GetVietnamBanksAsync() => GetCatalogBanksAsync("VN");

    sealed class BankCatalogJsonDto
    {
        public string name { get; set; } = "";
        public string swift { get; set; } = "";
    }

    public async Task<(bool ok, RecipientResponse? data, string error)> AddRecipientAsync(
        string name, string? phone, string note,
        string? countryCode,
        string? bankName = null, string? accountNumber = null, string? accountHolderName = null)
    {
        SetToken();
        var res = await _http.PostAsJsonAsync("api/recipient",
            new { name, phone, note, countryCode, bankName, accountNumber, accountHolderName });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<RecipientResponse>(), "");
        var err = await res.Content.ReadAsStringAsync();
        return (false, null, err.Length > 200 ? "Thêm thất bại" : err);
    }

    public async Task<(bool ok, RecipientResponse? data, string error)> UpdateRecipientAsync(
        int id, string name, string? phone, string note,
        string? countryCode,
        string? bankName = null, string? accountNumber = null, string? accountHolderName = null)
    {
        SetToken();
        var res = await _http.PutAsJsonAsync($"api/recipient/{id}",
            new { name, phone, note, countryCode, bankName, accountNumber, accountHolderName });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<RecipientResponse>(), "");
        var err = await res.Content.ReadAsStringAsync();
        return (false, null, err.Length > 200 ? "Cập nhật thất bại" : err);
    }

    public async Task<bool> DeleteRecipientAsync(int id)
    {
        SetToken();
        var res = await _http.DeleteAsync($"api/recipient/{id}");
        return res.IsSuccessStatusCode;
    }

    public async Task<(bool ok, TransactionResponse? data, string error)> TransferAsync(
        string receiverPhone, decimal amount, string note,
        string? receiverBankName = null, string? receiverAccountNumber = null)
    {
        SetToken();
        var res = await _http.PostAsJsonAsync("api/transaction/transfer",
            new { receiverPhone, amount, note, receiverBankName, receiverAccountNumber });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<TransactionResponse>(), "");
        return (false, null, "Chuyển tiền thất bại. Kiểm tra số dư hoặc SĐT.");
    }

    public async Task<List<TransactionResponse>> GetHistoryAsync(int page = 1)
    {
        SetToken();
        return await _http.GetFromJsonAsync<List<TransactionResponse>>(
            $"api/transaction/history?page={page}&pageSize=20") ?? [];
    }
}
