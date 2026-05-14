using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blazored.LocalStorage;

namespace SendPay.Web.Services;

public record AuthResponse(
    int UserId,
    string Token,
    string FullName,
    string Email,
    string Phone,
    bool IsAdmin = false,
    string? RefreshToken = null,
    DateTime? AccessTokenExpiresAtUtc = null);
public record WalletResponse(string FullName, string Phone, decimal Balance);
public record TransactionResponse(int Id, int SenderId, string SenderName, string ReceiverName,
    decimal Amount, decimal Fee, string Note, int Type, int Status, DateTime CreatedAt,
    string? ReceiverBankName = null, string? ReceiverAccountNumber = null);

public record AdminStatsResponse(
    int TotalUsers, int ActiveUsers,
    int TotalTransactions, int TodayTransactions,
    decimal TotalVolume, decimal TodayVolume);

public record AdminUserResponse(
    int Id, string FullName, string Email, string Phone,
    decimal Balance, bool IsActive, bool IsAdmin, DateTime CreatedAt,
    string? JapanBankName, string? JapanBankTopUpUrl);

public record AdminTransactionResponse(
    int Id, string SenderName, string ReceiverName,
    decimal Amount, string Note, string Type, string Status, DateTime CreatedAt);

public record AdminTopUpIntentResponse(
    int Id,
    int UserId,
    string UserName,
    string UserEmail,
    decimal ExpectedAmount,
    string ReferenceCode,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? MatchedAt,
    int? TransactionId,
    int? BankStatementLineId);

public record AdminDailyStatResponse(
    DateTime StatDate,
    string TransactionType,
    string Status,
    int Count,
    decimal TotalAmount,
    decimal TotalFee,
    DateTime ComputedAt);

public record WalletTopUpResponse(
    string Mode,
    WalletResponse? Wallet,
    int? IntentId,
    string? ReferenceCode,
    DateTime? ExpiresAt,
    decimal? ExpectedAmount);

public record RecipientResponse(
    int Id, string Name, string? Phone, string Note,
    string? CountryCode,
    string? BankName, string? AccountNumber, string? AccountHolderName, string? SwiftBic, DateTime CreatedAt);

public record CatalogBankOption(string Name, string Swift);
public record UserProfileResponse(int Id, string FullName, string Email, string Phone, decimal Balance, DateTime CreatedAt,
    string? JapanBankName, string? JapanBankTopUpUrl);
public record CurrencyRate(string Code, string Flag, string Country, decimal Rate, string Change, bool Up);
public record ExchangeRateResponse(string Date, List<CurrencyRate> Rates);

public record JapanBankOptionDto(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("nameJa")] string NameJa,
    [property: JsonPropertyName("nameEn")] string NameEn,
    [property: JsonPropertyName("emoji")] string Emoji);

public record BankLinkStartResponse(
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("authorizeUrl")] string AuthorizeUrl,
    [property: JsonPropertyName("expiresAt")] DateTime ExpiresAt);

public record FakeBankApproveResponse(
    [property: JsonPropertyName("redirectUrl")] string RedirectUrl,
    [property: JsonPropertyName("linkId")] int LinkId,
    [property: JsonPropertyName("bankCode")] string BankCode,
    [property: JsonPropertyName("bankName")] string BankName,
    [property: JsonPropertyName("accountMasked")] string AccountMasked);

public record UserBankLinkDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("bankCode")] string BankCode,
    [property: JsonPropertyName("bankName")] string BankName,
    [property: JsonPropertyName("accountMasked")] string AccountMasked,
    [property: JsonPropertyName("isPrimary")] bool IsPrimary,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("linkedAt")] DateTime LinkedAt);

public record ReceiverLookupDto(
    [property: JsonPropertyName("found")] bool Found,
    [property: JsonPropertyName("fullName")] string? FullName,
    [property: JsonPropertyName("isSelf")] bool IsSelf,
    [property: JsonPropertyName("savedRecipientId")] int? SavedRecipientId,
    [property: JsonPropertyName("matchKind")] string? MatchKind,
    [property: JsonPropertyName("bankDisplay")] string? BankDisplay,
    [property: JsonPropertyName("resolvedPhone")] string? ResolvedPhone);

public class ApiService(HttpClient http, ILocalStorageService localStorage)
{
    static readonly TimeSpan AccessRefreshSkew = TimeSpan.FromMinutes(2);

    public async Task PersistAuthAsync(AuthResponse data)
    {
        await localStorage.SetItemAsync("token", data.Token);
        if (!string.IsNullOrEmpty(data.RefreshToken))
            await localStorage.SetItemAsync("sp_refresh", data.RefreshToken);
        if (data.AccessTokenExpiresAtUtc is { } exp)
            await localStorage.SetItemAsync("sp_access_exp", exp.ToUniversalTime().ToString("o"));
        await localStorage.SetItemAsync("fullName", data.FullName);
        await localStorage.SetItemAsync("isAdmin", data.IsAdmin);
        await localStorage.SetItemAsync("userId", data.UserId);
    }

    async Task EnsureFreshAccessTokenAsync()
    {
        var expStr = await localStorage.GetItemAsync<string>("sp_access_exp");
        if (string.IsNullOrEmpty(expStr)) return;
        if (!DateTime.TryParse(expStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expUtc))
            return;
        if (expUtc.ToUniversalTime() > DateTime.UtcNow.Add(AccessRefreshSkew)) return;

        var refresh = await localStorage.GetItemAsync<string>("sp_refresh");
        if (string.IsNullOrEmpty(refresh)) return;

        var res = await http.PostAsJsonAsync("api/auth/refresh", new { refreshToken = refresh });
        if (!res.IsSuccessStatusCode) return;

        var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
        if (data != null)
            await PersistAuthAsync(data);
    }

    private async Task SetAuthHeader()
    {
        await EnsureFreshAccessTokenAsync();
        var token = await localStorage.GetItemAsync<string>("token");
        http.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>Thu hồi JWT/refresh trên server và xóa bộ nhớ cục bộ.</summary>
    public async Task LogoutAsync()
    {
        var token = await localStorage.GetItemAsync<string>("token");
        http.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
        try
        {
            await http.PostAsync("api/auth/logout", null);
        }
        catch
        {
            /* best-effort */
        }

        http.DefaultRequestHeaders.Authorization = null;
        await localStorage.RemoveItemAsync("token");
        await localStorage.RemoveItemAsync("sp_refresh");
        await localStorage.RemoveItemAsync("sp_access_exp");
        await localStorage.RemoveItemAsync("fullName");
        await localStorage.RemoveItemAsync("isAdmin");
        await localStorage.RemoveItemAsync("userId");
    }

    public async Task<(bool ok, AuthResponse? data, string error)> RegisterAsync(
        string fullName, string email, string phone, string password)
    {
        var res = await http.PostAsJsonAsync("api/auth/register",
            new { fullName, email, phone, password });

        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<AuthResponse>(), "");

        return (false, null, await ReadErrorMessageAsync(res, "Lỗi không xác định"));
    }

    public async Task<(bool ok, AuthResponse? data, string error)> LoginAsync(
        string email, string password)
    {
        var res = await http.PostAsJsonAsync("api/auth/login", new { email, password });

        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<AuthResponse>(), "");

        return (false, null, await ReadErrorMessageAsync(res, "Sai email hoặc mật khẩu"));
    }

    public async Task<WalletResponse?> GetWalletAsync()
    {
        await SetAuthHeader();
        return await http.GetFromJsonAsync<WalletResponse>("api/wallet");
    }

    public async Task<ReceiverLookupDto?> LookupTransferCounterpartyAsync(
        string? phone, string? accountNumber = null, string? bankName = null)
    {
        await SetAuthHeader();
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(phone))
            parts.Add($"phone={Uri.EscapeDataString(phone.Trim())}");
        if (!string.IsNullOrWhiteSpace(accountNumber))
            parts.Add($"accountNumber={Uri.EscapeDataString(accountNumber.Trim())}");
        if (!string.IsNullOrWhiteSpace(bankName))
            parts.Add($"bankName={Uri.EscapeDataString(bankName.Trim())}");
        if (parts.Count == 0) return null;
        var qs = string.Join("&", parts);
        return await http.GetFromJsonAsync<ReceiverLookupDto>($"api/user/receiver-lookup?{qs}");
    }

    public async Task<(bool ok, WalletTopUpResponse? data, string error)> TopUpAsync(decimal amount)
    {
        await SetAuthHeader();
        var res = await http.PostAsJsonAsync("api/wallet/topup", new { amount });

        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<WalletTopUpResponse>(), "");

        return (false, null, await ReadErrorMessageAsync(res, "Nạp tiền thất bại"));
    }

    public async Task<(bool ok, TransactionResponse? data, string error)> TransferAsync(
        string receiverPhone, decimal amount, string note,
        string? receiverBankName = null, string? receiverAccountNumber = null)
    {
        await SetAuthHeader();
        var res = await http.PostAsJsonAsync("api/transaction/transfer",
            new { receiverPhone, amount, note, receiverBankName, receiverAccountNumber });

        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<TransactionResponse>(), "");

        return (false, null, await ReadErrorMessageAsync(res, "Chuyển tiền thất bại"));
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

    public async Task<List<CatalogBankOption>> GetCatalogBanksAsync(string countryCode)
    {
        await SetAuthHeader();
        var c = (countryCode ?? "").Trim().ToUpperInvariant();
        var raw = await http.GetFromJsonAsync<List<BankCatalogJsonDto>>(
            $"api/reference/banks/{Uri.EscapeDataString(c)}");
        return raw?.Select(x => new CatalogBankOption(x.name, x.swift)).ToList() ?? [];
    }

    /// <summary>Tương thích: tương đương GetCatalogBanksAsync("VN").</summary>
    public Task<List<CatalogBankOption>> GetVietnamBanksAsync() => GetCatalogBanksAsync("VN");

    // ── Japan banks + bank-link (mock provider) ────────────────
    public async Task<List<JapanBankOptionDto>> SearchJapanBanksAsync(string? query, int max = 25)
    {
        await SetAuthHeader();
        var url = $"api/reference/japan-banks?max={max}";
        if (!string.IsNullOrWhiteSpace(query))
            url += $"&q={Uri.EscapeDataString(query.Trim())}";
        return await http.GetFromJsonAsync<List<JapanBankOptionDto>>(url) ?? [];
    }

    public async Task<(bool ok, BankLinkStartResponse? data, string error)> StartBankLinkAsync(string bankCode, string? returnUrl)
    {
        await SetAuthHeader();
        var res = await http.PostAsJsonAsync("api/bank-link/start", new { bankCode, returnUrl });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<BankLinkStartResponse>(), "");
        return (false, null, await ReadErrorMessageAsync(res, "Không khởi tạo được phiên liên kết."));
    }

    public async Task<(bool ok, FakeBankApproveResponse? data, string error)> FakeBankApproveAsync(string state, string accountNo, string loginId)
    {
        var res = await http.PostAsJsonAsync("api/bank-link/fake-approve", new { state, accountNo, loginId });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<FakeBankApproveResponse>(), "");
        return (false, null, await ReadErrorMessageAsync(res, "Xác nhận thất bại."));
    }

    public async Task<List<UserBankLinkDto>> GetMyBankLinksAsync()
    {
        await SetAuthHeader();
        return await http.GetFromJsonAsync<List<UserBankLinkDto>>("api/bank-link/me") ?? [];
    }

    public async Task<UserBankLinkDto?> GetPrimaryBankLinkAsync()
    {
        await SetAuthHeader();
        var res = await http.GetAsync("api/bank-link/primary");
        if (res.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<UserBankLinkDto>();
    }

    public async Task<bool> UnlinkBankAsync(int linkId)
    {
        await SetAuthHeader();
        var res = await http.DeleteAsync($"api/bank-link/{linkId}");
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> SetPrimaryBankAsync(int linkId)
    {
        await SetAuthHeader();
        var res = await http.PutAsync($"api/bank-link/{linkId}/primary", null);
        return res.IsSuccessStatusCode;
    }

    sealed class BankCatalogJsonDto
    {
        public string name { get; set; } = "";
        public string swift { get; set; } = "";
    }

    // ── Recipients ─────────────────────────────────────────────
    public async Task<List<RecipientResponse>> GetRecipientsAsync()
    {
        await SetAuthHeader();
        return await http.GetFromJsonAsync<List<RecipientResponse>>("api/recipient") ?? [];
    }

    public async Task<RecipientResponse?> GetRecipientByIdAsync(int id)
    {
        await SetAuthHeader();
        return await http.GetFromJsonAsync<RecipientResponse>($"api/recipient/{id}");
    }

    public async Task<(bool ok, RecipientResponse? data, string error)> AddRecipientAsync(
        string name, string? phone, string note,
        string? countryCode = null,
        string? bankName = null, string? accountNumber = null, string? accountHolderName = null)
    {
        await SetAuthHeader();
        var res = await http.PostAsJsonAsync("api/recipient",
            new { name, phone, note, countryCode, bankName, accountNumber, accountHolderName });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<RecipientResponse>(), "");
        return (false, null, await ReadErrorMessageAsync(res, "Thêm thất bại"));
    }

    public async Task<(bool ok, RecipientResponse? data, string error)> UpdateRecipientAsync(
        int id, string name, string? phone, string note,
        string? countryCode = null,
        string? bankName = null, string? accountNumber = null, string? accountHolderName = null)
    {
        await SetAuthHeader();
        var res = await http.PutAsJsonAsync($"api/recipient/{id}",
            new { name, phone, note, countryCode, bankName, accountNumber, accountHolderName });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<RecipientResponse>(), "");
        return (false, null, await ReadErrorMessageAsync(res, "Cập nhật thất bại"));
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
        string fullName, string email, string phone,
        string? japanBankName, string? japanBankTopUpUrl)
    {
        await SetAuthHeader();
        var res = await http.PutAsJsonAsync("api/user/profile",
            new { fullName, email, phone, japanBankName, japanBankTopUpUrl });
        if (res.IsSuccessStatusCode) return (true, "");
        return (false, await ReadErrorMessageAsync(res, "Cập nhật thất bại"));
    }

    public async Task<(bool ok, string error)> ChangePasswordAsync(
        string currentPassword, string newPassword)
    {
        await SetAuthHeader();
        var res = await http.PutAsJsonAsync("api/user/password", new { currentPassword, newPassword });
        if (res.IsSuccessStatusCode) return (true, "");
        return (false, await ReadErrorMessageAsync(res, "Đổi mật khẩu thất bại"));
    }

    public async Task<(bool ok, string error)> UpdateAddressAsync(
        string postalCode, string prefecture, string city, string streetAddress)
    {
        await SetAuthHeader();
        var res = await http.PutAsJsonAsync("api/user/address",
            new { postalCode, prefecture, city, streetAddress });
        if (res.IsSuccessStatusCode) return (true, "");
        return (false, await ReadErrorMessageAsync(res, ""));
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

        return (false, null, await ReadErrorMessageAsync(res, "Thao tác thất bại"));
    }

    public async Task<(bool ok, AdminUserResponse? data, string error)> AdminUpdateUserAsync(
        int id, string fullName, string email, string phone, decimal? balance,
        string? japanBankName, string? japanBankTopUpUrl)
    {
        await SetAuthHeader();
        var res = await http.PutAsJsonAsync($"api/admin/users/{id}",
            new { fullName, email, phone, balance, japanBankName, japanBankTopUpUrl });
        if (res.IsSuccessStatusCode)
            return (true, await res.Content.ReadFromJsonAsync<AdminUserResponse>(), "");
        return (false, null, await ReadErrorMessageAsync(res, "Cập nhật thất bại"));
    }

    public async Task<(bool ok, string error)> AdminDeleteUserAsync(int id)
    {
        await SetAuthHeader();
        var res = await http.DeleteAsync($"api/admin/users/{id}");
        if (res.IsSuccessStatusCode) return (true, "");
        return (false, await ReadErrorMessageAsync(res, "Xóa thất bại"));
    }

    public async Task<List<AdminTransactionResponse>> GetAdminTransactionsAsync(int page = 1)
    {
        await SetAuthHeader();
        return await http.GetFromJsonAsync<List<AdminTransactionResponse>>(
            $"api/admin/transactions?page={page}&pageSize=15") ?? [];
    }

    public async Task<List<AdminTopUpIntentResponse>> GetAdminTopUpIntentsAsync(
        string? status = null, int page = 1)
    {
        await SetAuthHeader();
        var url = $"api/admin/topup-intents?page={page}&pageSize=30";
        if (!string.IsNullOrWhiteSpace(status))
            url += $"&status={Uri.EscapeDataString(status)}";
        return await http.GetFromJsonAsync<List<AdminTopUpIntentResponse>>(url) ?? [];
    }

    public async Task<(bool ok, string error)> AdminConfirmTopUpIntentAsync(int intentId)
    {
        await SetAuthHeader();
        var res = await http.PostAsync($"api/admin/topup-intents/{intentId}/confirm", null);
        if (res.IsSuccessStatusCode) return (true, "");
        return (false, await ReadErrorMessageAsync(res, "Thao tác thất bại"));
    }

    public async Task<(bool ok, int imported, string error)> ImportBankStatementLineAsync(
        DateTime bookingDate, decimal amount, string memo, string? creditAccount = null)
    {
        await SetAuthHeader();
        var res = await http.PostAsJsonAsync("api/admin/bank-statement-lines",
            new
            {
                lines = new[]
                {
                    new { bookingDate, amount, memo, creditAccount }
                }
            });
        if (res.IsSuccessStatusCode)
        {
            var doc = await res.Content.ReadFromJsonAsync<JsonElement>();
            if (doc.TryGetProperty("imported", out var imp) && imp.TryGetInt32(out var n))
                return (true, n, "");
            return (true, 1, "");
        }

        return (false, 0, await ReadErrorMessageAsync(res, "Import thất bại"));
    }

    public async Task<List<AdminDailyStatResponse>> GetAdminDailyStatsAsync(
        DateTime? from = null, DateTime? to = null)
    {
        await SetAuthHeader();
        var parts = new List<string>();
        if (from.HasValue)
            parts.Add($"from={Uri.EscapeDataString(from.Value.ToUniversalTime().ToString("yyyy-MM-dd"))}");
        if (to.HasValue)
            parts.Add($"to={Uri.EscapeDataString(to.Value.ToUniversalTime().ToString("yyyy-MM-dd"))}");
        var url = parts.Count > 0 ? $"api/admin/daily-stats?{string.Join("&", parts)}" : "api/admin/daily-stats";
        return await http.GetFromJsonAsync<List<AdminDailyStatResponse>>(url) ?? [];
    }

    public async Task<(bool ok, string error)> AdminRebuildDailyStatsAsync(DateTime? utcDay = null)
    {
        await SetAuthHeader();
        var url = utcDay.HasValue
            ? $"api/admin/daily-stats/rebuild?utcDay={Uri.EscapeDataString(utcDay.Value.ToString("O"))}"
            : "api/admin/daily-stats/rebuild";
        var res = await http.PostAsync(url, null);
        if (res.IsSuccessStatusCode) return (true, "");
        return (false, await ReadErrorMessageAsync(res, "Rebuild thất bại"));
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage res, string fallback)
    {
        var body = await res.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            var code = (int)res.StatusCode;
            var reason = res.ReasonPhrase ?? "";
            return code > 0 ? $"{code} {reason}".Trim() : fallback;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            static string? strProp(JsonElement el, string a, string b)
            {
                if (el.TryGetProperty(a, out var x) && x.ValueKind == JsonValueKind.String)
                    return x.GetString();
                if (el.TryGetProperty(b, out var y) && y.ValueKind == JsonValueKind.String)
                    return y.GetString();
                return null;
            }

            var msg = strProp(root, "message", "Message")
                      ?? strProp(root, "detail", "Detail")
                      ?? strProp(root, "title", "Title");
            if (!string.IsNullOrWhiteSpace(msg))
                return msg!;

            if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Object)
            {
                var parts = new List<string>();
                foreach (var prop in errs.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in prop.Value.EnumerateArray())
                            if (item.ValueKind == JsonValueKind.String && item.GetString() is { } s)
                                parts.Add(s);
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.String && prop.Value.GetString() is { } one)
                        parts.Add(one);
                }
                var joined = string.Join(" ", parts.Where(s => s.Length > 0));
                if (!string.IsNullOrWhiteSpace(joined))
                    return joined;
            }
        }
        catch (JsonException) { /* body không phải JSON */ }

        return body.Length > 280 ? body[..277] + "…" : body;
    }
}
