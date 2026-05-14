using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SendPay.Mobile.Services;

namespace SendPay.Mobile.Pages;

[QueryProperty(nameof(RecipientIdQuery), "recipientId")]
[QueryProperty(nameof(PhoneQuery), "phone")]
public partial class TransferPage : ContentPage
{
    private readonly ApiService _api;
    CancellationTokenSource? _phoneLookupCts;
    List<RecipientResponse> _recipients = [];
    private readonly Dictionary<string, List<CatalogBankOption>> _bankCache = new(StringComparer.OrdinalIgnoreCase);
    List<CatalogBankOption> _catalogBanks = [];
    bool _skipNextSuggestUpdate;

    string _recipientIdQuery = "";
    public string RecipientIdQuery
    {
        get => _recipientIdQuery;
        set
        {
            _recipientIdQuery = value ?? "";
            _ = LoadFromRecipientAsync(_recipientIdQuery);
        }
    }

    public string PhoneQuery
    {
        set =>
            MainThread.BeginInvokeOnMainThread(() => PhoneEntry.Text = Uri.UnescapeDataString(value ?? ""));
    }

    public TransferPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        foreach (var label in RecipientCatalogCountries.PickerLabels)
            CountryPicker.Items.Add(label);
        CountryPicker.SelectedIndex = 0;
        ApplyCountryBankUi();
    }

    bool IsCatalogCountrySelected =>
        CountryPicker.SelectedIndex >= 0 &&
        CountryPicker.SelectedIndex < RecipientCatalogCountries.Codes.Length;

    async Task LoadCatalogBanksAsync()
    {
        if (!IsCatalogCountrySelected)
        {
            _catalogBanks = [];
            return;
        }

        var code = RecipientCatalogCountries.CodeFromPickerIndex(CountryPicker.SelectedIndex);
        if (_bankCache.TryGetValue(code, out var hit))
        {
            _catalogBanks = hit;
            return;
        }

        try
        {
            var list = await _api.GetCatalogBanksAsync(code);
            _bankCache[code] = list;
            _catalogBanks = list;
        }
        catch
        {
            _catalogBanks = [];
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { _recipients = await _api.GetRecipientsAsync(); }
        catch { _recipients = []; }
        try { await LoadCatalogBanksAsync(); }
        catch { _catalogBanks = []; }
    }

    void ApplyCountryBankUi()
    {
        CatalogBankSearchLabel.Text = IsCatalogCountrySelected
            ? "Tìm ngân hàng (danh sách)"
            : "Ngân hàng";
        VnBankLayout.IsVisible = IsCatalogCountrySelected;
        OtherBankLayout.IsVisible = !IsCatalogCountrySelected;
    }

    async void OnCountryPickerChanged(object? sender, EventArgs e)
    {
        ApplyCountryBankUi();
        if (IsCatalogCountrySelected)
            OtherBankEntry.Text = "";
        else
            VnBankSearchEntry.Text = "";
        VnBankSuggestBorder.IsVisible = false;
        VnBankSuggestStack.Children.Clear();
        try { await LoadCatalogBanksAsync(); }
        catch { _catalogBanks = []; }
        _ = DebouncedLookupAsync();
    }

    void OnVnBankSearchChanged(object? sender, TextChangedEventArgs e)
    {
        var q = (VnBankSearchEntry.Text ?? "").Trim();
        VnBankSuggestStack.Children.Clear();
        if (string.IsNullOrEmpty(q) || _catalogBanks.Count == 0)
        {
            VnBankSuggestBorder.IsVisible = false;
            _ = DebouncedLookupAsync();
            return;
        }

        var hits = _catalogBanks.Where(b => b.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).Take(20).ToList();
        if (hits.Count == 0)
        {
            VnBankSuggestBorder.IsVisible = false;
            _ = DebouncedLookupAsync();
            return;
        }

        VnBankSuggestBorder.IsVisible = true;
        foreach (var bank in hits)
        {
            var captured = bank;
            var btn = new Button
            {
                Text = bank.Name,
                BackgroundColor = Colors.White,
                TextColor = Colors.Black,
                BorderColor = Color.FromArgb("#e2e8f0"),
                BorderWidth = 1,
                CornerRadius = 6,
                Padding = new Thickness(8, 6),
                FontSize = 12,
                LineBreakMode = LineBreakMode.WordWrap
            };
            btn.Clicked += async (_, _) =>
            {
                VnBankSearchEntry.Text = captured.Name;
                VnBankSuggestBorder.IsVisible = false;
                VnBankSuggestStack.Children.Clear();
                await DebouncedLookupAsync();
            };
            VnBankSuggestStack.Children.Add(btn);
        }

        _ = DebouncedLookupAsync();
    }

    string BankForApi()
    {
        if (IsCatalogCountrySelected)
            return (VnBankSearchEntry.Text ?? "").Trim();
        return (OtherBankEntry.Text ?? "").Trim();
    }

    async Task LoadFromRecipientAsync(string raw)
    {
        if (!int.TryParse(raw, out var rid)) return;
        var rec = await _api.GetRecipientByIdAsync(rid);
        if (rec == null) return;
        _skipNextSuggestUpdate = true;
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            PhoneEntry.Text = rec.Phone ?? "";
            CountryPicker.SelectedIndex = RecipientCatalogCountries.PickerIndexFromCode(
                RecipientCatalogCountries.FormCountryFromRecipient(rec));
            ApplyCountryBankUi();
            try { await LoadCatalogBanksAsync(); }
            catch { _catalogBanks = []; }
            if (IsCatalogCountrySelected)
            {
                VnBankSearchEntry.Text = rec.BankName ?? "";
                OtherBankEntry.Text = "";
            }
            else
            {
                OtherBankEntry.Text = rec.BankName ?? "";
                VnBankSearchEntry.Text = "";
            }
            AccountEntry.Text = rec.AccountNumber ?? "";
            HolderEntry.Text = rec.AccountHolderName ?? "";
            NoteEntry.Text = rec.Note ?? "";
            SuggestBorder.IsVisible = false;
            SuggestStack.Children.Clear();
            VnBankSuggestBorder.IsVisible = false;
            VnBankSuggestStack.Children.Clear();
        });
        await DebouncedLookupAsync();
    }

    async void OnTransferClicked(object sender, EventArgs e)
    {
        MsgLabel.IsVisible = false;
        if (!decimal.TryParse(AmountEntry.Text?.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
        {
            ShowMsg("Số tiền không hợp lệ", "#dc2626");
            return;
        }

        var bank = BankForApi();
        if (IsCatalogCountrySelected &&
            !_catalogBanks.Exists(b => string.Equals(b.Name, bank, StringComparison.OrdinalIgnoreCase)))
        {
            ShowMsg("Chọn ngân hàng từ danh sách gợi ý đúng quốc gia.", "#dc2626");
            return;
        }

        if (!IsCatalogCountrySelected && string.IsNullOrWhiteSpace(bank))
        {
            ShowMsg("Nhập tên ngân hàng người nhận.", "#dc2626");
            return;
        }

        var phD = DigitsOnly(PhoneEntry.Text);
        var acK = AccountKey(AccountEntry.Text);
        if (phD.Length < 8 && acK.Length < 6)
        {
            ShowMsg("Nhập SĐT (≥8 số) hoặc STK (≥6 số).", "#dc2626");
            return;
        }

        var lookup = await _api.LookupTransferCounterpartyAsync(
            string.IsNullOrWhiteSpace(PhoneEntry.Text) ? null : PhoneEntry.Text.Trim(),
            string.IsNullOrWhiteSpace(AccountEntry.Text) ? null : AccountEntry.Text.Trim(),
            string.IsNullOrWhiteSpace(bank) ? null : bank);

        if (lookup?.IsSelf == true)
        {
            ShowMsg("Bạn không thể chuyển tiền cho chính mình.", "#dc2626");
            return;
        }

        if (lookup is not { Found: true, FullName: { } fn } || string.IsNullOrWhiteSpace(fn))
        {
            ShowMsg("Không tìm thấy người nhận trong danh bạ hoặc SendPay.", "#dc2626");
            return;
        }

        var eff = string.IsNullOrWhiteSpace(lookup.ResolvedPhone)
            ? PhoneEntry.Text?.Trim() ?? ""
            : lookup.ResolvedPhone.Trim();
        if (DigitsOnly(eff).Length < 8)
        {
            ShowMsg("Chuyển ví cần SĐT người nhận (≥8 số). Thêm SĐT vào danh bạ hoặc nhập SĐT SendPay.", "#dc2626");
            return;
        }

        TransferBtn.IsEnabled = false;
        TransferBtn.Text = "Đang xử lý...";

        var rb = string.IsNullOrWhiteSpace(bank) ? null : bank;
        var racct = string.IsNullOrWhiteSpace(AccountEntry.Text) ? null : AccountEntry.Text.Trim();
        var (ok, data, err) = await _api.TransferAsync(eff, amount, NoteEntry.Text ?? "", rb, racct);

        TransferBtn.IsEnabled = true;
        TransferBtn.Text = "Chuyển tiền";

        if (ok && data != null)
        {
            ShowMsg($"✓ Đã chuyển {data.Amount:N0} JPY cho {data.ReceiverName} thành công!", "#16a34a");
            PhoneEntry.Text = AmountEntry.Text = NoteEntry.Text = "";
            VnBankSearchEntry.Text = OtherBankEntry.Text = AccountEntry.Text = HolderEntry.Text = "";
            CountryPicker.SelectedIndex = 0;
            ApplyCountryBankUi();
            try { await LoadCatalogBanksAsync(); }
            catch { _catalogBanks = []; }
            ReceiverHintLabel.Text = "";
            ReceiverHintLabel.IsVisible = false;
            SuggestBorder.IsVisible = false;
            SuggestStack.Children.Clear();
            VnBankSuggestBorder.IsVisible = false;
            VnBankSuggestStack.Children.Clear();
        }
        else ShowMsg(err, "#dc2626");
    }

    void ShowMsg(string msg, string color)
    {
        MsgLabel.Text = msg;
        MsgLabel.TextColor = Color.FromArgb(color);
        MsgLabel.IsVisible = true;
    }

    async void OnReceiverFieldTextChanged(object? sender, TextChangedEventArgs e) => await DebouncedLookupAsync();

    async Task DebouncedLookupAsync()
    {
        _phoneLookupCts?.Cancel();
        _phoneLookupCts?.Dispose();
        _phoneLookupCts = new CancellationTokenSource();
        var token = _phoneLookupCts.Token;
        try { await Task.Delay(400, token); }
        catch (TaskCanceledException) { return; }

        var bank = BankForApi();
        var phD = DigitsOnly(PhoneEntry.Text);
        var acK = AccountKey(AccountEntry.Text);
        ReceiverLookupDto? res = null;
        if (phD.Length >= 8 || acK.Length >= 6)
        {
            res = await _api.LookupTransferCounterpartyAsync(
                string.IsNullOrWhiteSpace(PhoneEntry.Text) ? null : PhoneEntry.Text.Trim(),
                string.IsNullOrWhiteSpace(AccountEntry.Text) ? null : AccountEntry.Text.Trim(),
                string.IsNullOrWhiteSpace(bank) ? null : bank);
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (phD.Length < 8 && acK.Length < 6)
            {
                ReceiverHintLabel.IsVisible = false;
                ReceiverHintLabel.Text = "";
            }
            else
            {
                ReceiverHintLabel.IsVisible = true;
                if (res?.IsSelf == true)
                {
                    ReceiverHintLabel.TextColor = Color.FromArgb("#dc2626");
                    ReceiverHintLabel.Text = "Bạn không thể chuyển tiền cho chính mình.";
                }
                else if (res is { Found: true, FullName: { } n } && !string.IsNullOrWhiteSpace(n))
                {
                    ReceiverHintLabel.TextColor = Color.FromArgb("#16a34a");
                    var bd = string.IsNullOrEmpty(res.BankDisplay) ? "" : $" · {res.BankDisplay}";
                    ReceiverHintLabel.Text = $"Tài khoản nhận: {n}{bd}";
                }
                else
                {
                    ReceiverHintLabel.TextColor = Color.FromArgb("#b45309");
                    ReceiverHintLabel.Text = "Không tìm thấy trong danh bạ / SendPay.";
                }
            }

            UpdateRecipientSuggestButtons();
        });
    }

    void UpdateRecipientSuggestButtons()
    {
        if (_skipNextSuggestUpdate)
        {
            _skipNextSuggestUpdate = false;
            return;
        }

        var terms = new List<string>();
        void T(string? s)
        {
            s = (s ?? "").Trim();
            if (s.Length >= 2) terms.Add(s);
        }

        T(PhoneEntry.Text);
        T(BankForApi());
        T(AccountEntry.Text);
        T(HolderEntry.Text);

        SuggestStack.Children.Clear();
        if (terms.Count == 0)
        {
            SuggestBorder.IsVisible = false;
            return;
        }

        var list = _recipients
            .Where(r => terms.Exists(t =>
                (r.Name?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Phone?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.BankName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.AccountNumber?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.AccountHolderName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false)))
            .Take(8)
            .ToList();

        if (list.Count == 0)
        {
            SuggestBorder.IsVisible = false;
            return;
        }

        SuggestBorder.IsVisible = true;
        foreach (var r in list)
        {
            var sub = new List<string>();
            if (!string.IsNullOrWhiteSpace(r.Phone)) sub.Add(r.Phone!);
            var bankAcct = string.Join(" · ", new[] { r.BankName, r.AccountNumber }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (!string.IsNullOrEmpty(bankAcct)) sub.Add(bankAcct);

            var b = new Button
            {
                Text = sub.Count == 0 ? r.Name : $"{r.Name}\n{string.Join(" · ", sub)}",
                BackgroundColor = Colors.White,
                TextColor = Colors.Black,
                BorderColor = Color.FromArgb("#e2e8f0"),
                BorderWidth = 1,
                CornerRadius = 8,
                Padding = new Thickness(10, 8),
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap
            };
            var captured = r;
            b.Clicked += async (_, _) => await ApplyRecipientFromBookAsync(captured);
            SuggestStack.Children.Add(b);
        }
    }

    async Task ApplyRecipientFromBookAsync(RecipientResponse r)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            PhoneEntry.Text = r.Phone ?? "";
            CountryPicker.SelectedIndex = RecipientCatalogCountries.PickerIndexFromCode(
                RecipientCatalogCountries.FormCountryFromRecipient(r));
            ApplyCountryBankUi();
            try { await LoadCatalogBanksAsync(); }
            catch { _catalogBanks = []; }
            if (IsCatalogCountrySelected)
            {
                VnBankSearchEntry.Text = r.BankName ?? "";
                OtherBankEntry.Text = "";
            }
            else
            {
                OtherBankEntry.Text = r.BankName ?? "";
                VnBankSearchEntry.Text = "";
            }
            AccountEntry.Text = r.AccountNumber ?? "";
            HolderEntry.Text = r.AccountHolderName ?? "";
            if (string.IsNullOrEmpty(NoteEntry.Text))
                NoteEntry.Text = r.Note ?? "";
            SuggestBorder.IsVisible = false;
            SuggestStack.Children.Clear();
            VnBankSuggestBorder.IsVisible = false;
            VnBankSuggestStack.Children.Clear();
        });
        _skipNextSuggestUpdate = true;
        await DebouncedLookupAsync();
    }

    static string AccountKey(string? s) =>
        string.IsNullOrEmpty(s) ? "" : string.Concat(s.Where(char.IsLetterOrDigit)).ToUpperInvariant();

    static string DigitsOnly(string? s) =>
        string.IsNullOrEmpty(s) ? "" : new string(s.Where(char.IsDigit).ToArray());
}
