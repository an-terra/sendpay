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

    public ObservableCollection<RecipientRow> Rows { get; } = new();

    public RecipientsPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ReloadAsync();
    }

    async Task ReloadAsync()
    {
        var list = await _api.GetRecipientsAsync();
        Rows.Clear();
        foreach (var r in list)
        {
            var detail = string.IsNullOrEmpty(r.Phone) ? "" : $"📱 {r.Phone}";
            if (!string.IsNullOrEmpty(r.BankName) || !string.IsNullOrEmpty(r.AccountNumber) || !string.IsNullOrEmpty(r.SwiftBic))
            {
                var tail = AccountKey(r.AccountNumber);
                tail = tail.Length <= 4 ? tail : tail[^4..];
                var bank = string.IsNullOrEmpty(r.BankName) ? "NH" : r.BankName;
                var sw = string.IsNullOrEmpty(r.SwiftBic) ? "" : $" · SWIFT {r.SwiftBic}";
                var ac = string.IsNullOrEmpty(tail) ? "" : $" · {bank}{sw} ****{tail}";
                detail += ac;
            }
            if (string.IsNullOrWhiteSpace(detail)) detail = "—";
            Rows.Add(new RecipientRow { Id = r.Id, Name = r.Name, Detail = detail });
        }
    }

    static string AccountKey(string? s) =>
        string.IsNullOrEmpty(s) ? "" : string.Concat(s.Where(char.IsLetterOrDigit)).ToUpperInvariant();

    static bool IsValidBic(string swift)
    {
        var s = string.Concat(swift.Trim().ToUpperInvariant().Where(c =>
            c is >= 'A' and <= 'Z' || char.IsDigit(c)));
        if (s.Length is not (8 or 11)) return false;
        for (var i = 0; i < 6; i++)
            if (!char.IsLetter(s[i])) return false;
        for (var i = 6; i < 8; i++)
            if (!char.IsLetterOrDigit(s[i])) return false;
        if (s.Length == 11)
            for (var i = 8; i < 11; i++)
                if (!char.IsLetterOrDigit(s[i])) return false;
        return true;
    }

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
            BankEntry.Text = r.BankName ?? "";
            AccountEntry.Text = r.AccountNumber ?? "";
            HolderEntry.Text = r.AccountHolderName ?? "";
            SwiftEntry.Text = r.SwiftBic ?? "";
            NoteEntryForm.Text = r.Note ?? "";
            CancelEditBtn.IsVisible = true;
            FormTitleLabel.Text = "Sửa người nhận";
            SaveRecipientBtn.Text = "Lưu thay đổi";
            StatusLabel.IsVisible = false;
        });
    }

    void OnCancelEditClicked(object sender, EventArgs e)
    {
        _editingId = null;
        NameEntry.Text = PhoneEntryForm.Text = BankEntry.Text = AccountEntry.Text = HolderEntry.Text = SwiftEntry.Text = NoteEntryForm.Text = "";
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
        var bk = string.IsNullOrWhiteSpace(BankEntry.Text) ? null : BankEntry.Text.Trim();
        var ac = string.IsNullOrWhiteSpace(AccountEntry.Text) ? null : AccountEntry.Text.Trim();
        var ho = string.IsNullOrWhiteSpace(HolderEntry.Text) ? null : HolderEntry.Text.Trim();
        var sw = string.IsNullOrWhiteSpace(SwiftEntry.Text) ? null : SwiftEntry.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            StatusLabel.Text = "Nhập tên hiển thị.";
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.IsVisible = true;
            return;
        }

        if (string.IsNullOrEmpty(bk) || string.IsNullOrEmpty(ac) || string.IsNullOrEmpty(ho) || string.IsNullOrEmpty(sw))
        {
            StatusLabel.Text = "Nhập đủ: ngân hàng, số TK, chủ TK, mã SWIFT/BIC.";
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

        if (!IsValidBic(sw))
        {
            StatusLabel.Text = "Mã SWIFT/BIC không hợp lệ (8 hoặc 11 ký tự).";
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.IsVisible = true;
            return;
        }

        if (_editingId is int eid)
        {
            var (ok, _, err) = await _api.UpdateRecipientAsync(eid, name, ph, note, bk, ac, ho, sw);
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
            var (ok, _, err) = await _api.AddRecipientAsync(name, ph, note, bk, ac, ho, sw);
            if (ok)
            {
                NameEntry.Text = PhoneEntryForm.Text = BankEntry.Text = AccountEntry.Text = HolderEntry.Text = SwiftEntry.Text = NoteEntryForm.Text = "";
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
