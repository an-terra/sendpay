using System.Collections.ObjectModel;
using System.Linq;
using SendPay.Mobile.Services;

namespace SendPay.Mobile.Pages;

public class RecipientRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Detail { get; set; } = "";
}

public partial class RecipientsPage : ContentPage
{
    private readonly ApiService _api;
    private int? _editingId;
    private readonly Dictionary<string, List<CatalogBankOption>> _bankCache = new(StringComparer.OrdinalIgnoreCase);
    private List<CatalogBankOption> _catalogBanks = [];

    public ObservableCollection<RecipientRow> Rows { get; } = new();

    public RecipientsPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        foreach (var label in RecipientCatalogCountries.PickerLabels)
            CountryPicker.Items.Add(label);
        CountryPicker.SelectedIndex = 0;
        BindingContext = this;
        ApplyCountryUi();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { await LoadCatalogBanksAsync(); }
        catch { _catalogBanks = []; }
        await ReloadAsync();
    }

    string CurrentCountryCode => RecipientCatalogCountries.CodeFromPickerIndex(CountryPicker.SelectedIndex);

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

        var code = CurrentCountryCode;
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

    void ApplyCountryUi()
    {
        var cat = IsCatalogCountrySelected;
        BankFieldLabel.Text = cat ? "Tìm ngân hàng (danh sách)" : "Tên ngân hàng";
        BankEntry.Placeholder = cat ? "Gõ để tìm…" : "Nhập tên ngân hàng";
        VnBankSuggestBorder.IsVisible = false;
        VnBankSuggestStack.Children.Clear();
        if (!cat)
            BankPickedLabel.IsVisible = false;
    }

    async void OnCountryPickerChanged(object? sender, EventArgs e)
    {
        BankEntry.Text = "";
        BankPickedLabel.IsVisible = false;
        VnBankSuggestBorder.IsVisible = false;
        VnBankSuggestStack.Children.Clear();
        ApplyCountryUi();
        try { await LoadCatalogBanksAsync(); }
        catch { _catalogBanks = []; }
    }

    void OnBankSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!IsCatalogCountrySelected)
        {
            VnBankSuggestBorder.IsVisible = false;
            VnBankSuggestStack.Children.Clear();
            return;
        }

        var q = (BankEntry.Text ?? "").Trim();
        VnBankSuggestStack.Children.Clear();
        if (string.IsNullOrEmpty(q) || _catalogBanks.Count == 0)
        {
            VnBankSuggestBorder.IsVisible = false;
            return;
        }

        var hits = _catalogBanks.Where(b => b.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).Take(20).ToList();
        if (hits.Count == 0)
        {
            VnBankSuggestBorder.IsVisible = false;
            return;
        }

        VnBankSuggestBorder.IsVisible = true;
        foreach (var b in hits)
        {
            var captured = b;
            var btn = new Button
            {
                Text = b.Name,
                BackgroundColor = Colors.White,
                TextColor = Colors.Black,
                BorderColor = Color.FromArgb("#e2e8f0"),
                BorderWidth = 1,
                CornerRadius = 6,
                Padding = new Thickness(8, 6),
                FontSize = 12,
                LineBreakMode = LineBreakMode.WordWrap
            };
            btn.Clicked += (_, _) => OnCatalogBankPicked(captured);
            VnBankSuggestStack.Children.Add(btn);
        }
    }

    void OnCatalogBankPicked(CatalogBankOption b)
    {
        BankEntry.Text = b.Name;
        BankPickedLabel.Text = "Đã chọn: " + b.Name;
        BankPickedLabel.IsVisible = true;
        VnBankSuggestBorder.IsVisible = false;
        VnBankSuggestStack.Children.Clear();
    }

    async Task ReloadAsync()
    {
        var list = await _api.GetRecipientsAsync();
        Rows.Clear();
        foreach (var r in list)
        {
            var detail = string.IsNullOrEmpty(r.Phone) ? "" : $"📱 {r.Phone}";
            if (!string.IsNullOrEmpty(r.BankName) || !string.IsNullOrEmpty(r.AccountNumber))
            {
                var tail = AccountKey(r.AccountNumber);
                tail = tail.Length <= 4 ? tail : tail[^4..];
                var bank = string.IsNullOrEmpty(r.BankName) ? "NH" : r.BankName;
                var ac = string.IsNullOrEmpty(tail) ? "" : $" · {bank} ****{tail}";
                detail += ac;
            }
            if (string.IsNullOrWhiteSpace(detail)) detail = "—";
            Rows.Add(new RecipientRow { Id = r.Id, Name = r.Name, Detail = detail });
        }
    }

    static string AccountKey(string? s) =>
        string.IsNullOrEmpty(s) ? "" : string.Concat(s.Where(char.IsLetterOrDigit)).ToUpperInvariant();

    async void OnSendClicked(object sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not int id) return;
        await Shell.Current.GoToAsync($"transfer?recipientId={id}");
    }

    async void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not int id) return;
        var r = await _api.GetRecipientByIdAsync(id);
        if (r == null) return;
        _editingId = id;
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            NameEntry.Text = r.Name;
            PhoneEntryForm.Text = r.Phone ?? "";
            CountryPicker.SelectedIndex = RecipientCatalogCountries.PickerIndexFromCode(
                RecipientCatalogCountries.FormCountryFromRecipient(r));
            ApplyCountryUi();
            try { await LoadCatalogBanksAsync(); }
            catch { _catalogBanks = []; }
            BankEntry.Text = r.BankName ?? "";
            BankPickedLabel.IsVisible = IsCatalogCountrySelected && !string.IsNullOrEmpty(r.BankName);
            BankPickedLabel.Text = "Đã chọn: " + (r.BankName ?? "");
            AccountEntry.Text = r.AccountNumber ?? "";
            HolderEntry.Text = r.AccountHolderName ?? "";
            NoteEntryForm.Text = r.Note ?? "";
            VnBankSuggestBorder.IsVisible = false;
            VnBankSuggestStack.Children.Clear();
            CancelEditBtn.IsVisible = true;
            FormTitleLabel.Text = "Sửa người nhận";
            SaveRecipientBtn.Text = "Lưu thay đổi";
            StatusLabel.IsVisible = false;
        });
    }

    void OnCancelEditClicked(object sender, EventArgs e)
    {
        _editingId = null;
        NameEntry.Text = PhoneEntryForm.Text = BankEntry.Text = AccountEntry.Text = HolderEntry.Text = NoteEntryForm.Text = "";
        CountryPicker.SelectedIndex = 0;
        ApplyCountryUi();
        BankPickedLabel.IsVisible = false;
        VnBankSuggestBorder.IsVisible = false;
        VnBankSuggestStack.Children.Clear();
        CancelEditBtn.IsVisible = false;
        FormTitleLabel.Text = "Thêm người nhận";
        SaveRecipientBtn.Text = "Thêm vào danh bạ";
        StatusLabel.IsVisible = false;
    }

    async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not int id) return;
        if (_editingId == id) OnCancelEditClicked(sender, e);
        await _api.DeleteRecipientAsync(id);
        await ReloadAsync();
    }

    async void OnSaveClicked(object sender, EventArgs e)
    {
        StatusLabel.IsVisible = false;
        var name = (NameEntry.Text ?? "").Trim();
        var ph = string.IsNullOrWhiteSpace(PhoneEntryForm.Text) ? null : PhoneEntryForm.Text.Trim();
        var note = (NoteEntryForm.Text ?? "").Trim();
        var ac = string.IsNullOrWhiteSpace(AccountEntry.Text) ? null : AccountEntry.Text.Trim();
        var ho = string.IsNullOrWhiteSpace(HolderEntry.Text) ? null : HolderEntry.Text.Trim();
        var country = CurrentCountryCode;
        var bk = string.IsNullOrWhiteSpace(BankEntry.Text) ? null : BankEntry.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            StatusLabel.Text = "Nhập tên hiển thị.";
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.IsVisible = true;
            return;
        }

        if (string.IsNullOrEmpty(bk) || string.IsNullOrEmpty(ac) || string.IsNullOrEmpty(ho))
        {
            StatusLabel.Text = "Nhập đủ: ngân hàng, số TK, chủ TK.";
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.IsVisible = true;
            return;
        }

        if (IsCatalogCountrySelected && !_catalogBanks.Exists(b => string.Equals(b.Name, bk, StringComparison.OrdinalIgnoreCase)))
        {
            StatusLabel.Text = "Chọn ngân hàng từ danh sách gợi ý đúng quốc gia.";
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.IsVisible = true;
            return;
        }

        if (AccountKey(ac).Length < 6)
        {
            StatusLabel.Text = "Số tài khoản cần ít nhất 6 ký tự.";
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.IsVisible = true;
            return;
        }

        if (_editingId is int eid)
        {
            var (ok, _, err) = await _api.UpdateRecipientAsync(eid, name, ph, note, country, bk, ac, ho);
            if (ok)
            {
                OnCancelEditClicked(sender, e);
                await ReloadAsync();
                StatusLabel.Text = "Đã cập nhật.";
                StatusLabel.TextColor = Color.FromArgb("#16a34a");
                StatusLabel.IsVisible = true;
            }
            else
            {
                StatusLabel.Text = err;
                StatusLabel.TextColor = Colors.Red;
                StatusLabel.IsVisible = true;
            }
        }
        else
        {
            var (ok, _, err) = await _api.AddRecipientAsync(name, ph, note, country, bk, ac, ho);
            if (ok)
            {
                NameEntry.Text = PhoneEntryForm.Text = BankEntry.Text = AccountEntry.Text = HolderEntry.Text = NoteEntryForm.Text = "";
                CountryPicker.SelectedIndex = 0;
                ApplyCountryUi();
                try { await LoadCatalogBanksAsync(); }
                catch { _catalogBanks = []; }
                BankPickedLabel.IsVisible = false;
                await ReloadAsync();
                StatusLabel.Text = "Đã thêm.";
                StatusLabel.TextColor = Color.FromArgb("#16a34a");
                StatusLabel.IsVisible = true;
            }
            else
            {
                StatusLabel.Text = err;
                StatusLabel.TextColor = Colors.Red;
                StatusLabel.IsVisible = true;
            }
        }
    }
}
