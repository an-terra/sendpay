using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace SendPay.Mobile.Services;

public record AuthResponse(string Token, string FullName, string Email, string Phone);
public record WalletResponse(string FullName, string Phone, decimal Balance);
public record TransactionResponse(int Id, string SenderName, string ReceiverName,
    decimal Amount, string Note, int Type, int Status, DateTime CreatedAt);

public class ApiService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "http://10.0.2.2:5050/"; // Android emulator → localhost

    public ApiService()
    {
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
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

    public async Task<(bool ok, WalletResponse? data, string error)> TopUpAsync(decimal amount)
    {
        SetToken();
        var res = await _http.PostAsJsonAsync("api/wallet/topup", new { amount });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<WalletResponse>(), "");
        return (false, null, "Nạp tiền thất bại");
    }

    public async Task<(bool ok, TransactionResponse? data, string error)> TransferAsync(
        string receiverPhone, decimal amount, string note)
    {
        SetToken();
        var res = await _http.PostAsJsonAsync("api/transaction/transfer", new { receiverPhone, amount, note });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<TransactionResponse>(), "");
        return (false, null, "Chuyển tiền thất bại. Kiểm tra số dư hoặc số điện thoại.");
    }

    public async Task<List<TransactionResponse>> GetHistoryAsync(int page = 1)
    {
        SetToken();
        return await _http.GetFromJsonAsync<List<TransactionResponse>>(
            $"api/transaction/history?page={page}&pageSize=20") ?? [];
    }
}
