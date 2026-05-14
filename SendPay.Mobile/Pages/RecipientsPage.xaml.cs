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
    private List<VietnamBankOption> _vnBanks = [];

    public ObservableCollection<RecipientRow> Rows { get; } = new();

    public RecipientsPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        CountryPicker.Items.Add("Việt Nam");
        CountryPicker.Items.Add("Khác");
        CountryPicker.SelectedIndex = 0;
        BindingContext = this;
        ApplyCountryUi();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { _vnBanks = await _api.GetVietnamBanksAsync(); }
        catch { _vnBanks = []; }
        await ReloadAsync();
    }

    void ApplyCountryUi()
    {
        var vn = CountryPicker.SelectedIndex == 0;
        BankFieldLabel.Text = vn ? "Tìm ngân hàng (Việt Nam)" : "Tên ngân hàng";
        BankEntry.Placeholder = vn ? "Gõ để tìm…" : "Nhập tên ngân hàng";
        VnBankSuggestBorder.IsVisible = false;
        VnBankSuggestStack.Children.Clear();
        if (!vn)
            BankPickedLabel.IsVisible = false;
    }

    void OnCountryPickerChanged(object? sender, EventArgs e)
    {
        BankEntry.Text = "";
        BankPickedLabel.IsVisible = false;
        VnBankSuggestBorder.IsVisible = false;
        VnBankSuggestStack.Children.Clear();
        ApplyCountryUi();
    }

    void OnBankSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (CountryPicker.SelectedIndex != 0)
        {
            VnBankSuggestBorder.IsVisible = false;
            VnBankSuggestStack.Children.Clear();
            return;
        }

        var q = (BankEntry.Text ?? "").Trim();
        VnBankSuggestStack.Children.Clear();
        if (string.IsNullOrEmpty(q) || _vnBanks.Count == 0)
        {
            VnBankSuggestBorder.IsVisible = false;
            return;
        }

        var hits = _vnBanks.Where(b => b.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).Take(20).ToList();
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
            btn.Clicked += (_, _) => OnVnBankPicked(captured);
            VnBankSuggestStack.Children.Add(btn);
        }
    }

    void OnVnBankPicked(VietnamBankOption b)
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

    static bool IsRecipientVietnam(RecipientResponse r) =>
        string.Equals(r.CountryCode, "VN", StringComparison.OrdinalIgnoreCase)
        || (string.IsNullOrEmpty(r.CountryCode) && !string.IsNullOrEmpty(r.SwiftBic));

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
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            NameEntry.Text = r.Name;
            PhoneEntryForm.Text = r.Phone ?? "";
            CountryPicker.SelectedIndex = IsRecipientVietnam(r) ? 0 : 1;
            ApplyCountryUi();
            BankEntry.Text = r.BankName ?? "";
            BankPickedLabel.IsVisible = IsRecipientVietnam(r) && !string.IsNullOrEmpty(r.BankName);
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
        var country = CountryPicker.SelectedIndex == 0 ? "VN" : "OTHER";
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

        if (country == "VN" && !_vnBanks.Exists(b => string.Equals(b.Name, bk, StringComparison.OrdinalIgnoreCase)))
        {
            StatusLabel.Text = "Chọn ngân hàng từ danh sách gợi ý (Việt Nam).";
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
