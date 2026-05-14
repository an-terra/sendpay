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
            if (!string.IsNullOrEmpty(r.BankName) || !string.IsNullOrEmpty(r.AccountNumber))
            {
                var tail = DigitsOnly(r.AccountNumber);
                tail = tail.Length <= 4 ? tail : tail[^4..];
                var bank = string.IsNullOrEmpty(r.BankName) ? "STK" : r.BankName;
                var ac = string.IsNullOrEmpty(tail) ? "" : $" · {bank} ****{tail}";
                detail += ac;
            }
            if (string.IsNullOrWhiteSpace(detail)) detail = "—";
            Rows.Add(new RecipientRow { Id = r.Id, Name = r.Name, Detail = detail });
        }
    }

    static string DigitsOnly(string? s) =>
        string.IsNullOrEmpty(s) ? "" : new string(s.Where(char.IsDigit).ToArray());

    async void OnSendClicked(object sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not int id) return;
        await Shell.Current.GoToAsync($"transfer?recipientId={id}");
    }

    async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not int id) return;
        await _api.DeleteRecipientAsync(id);
        await ReloadAsync();
    }

    async void OnAddClicked(object sender, EventArgs e)
    {
        StatusLabel.IsVisible = false;
        var name = (NameEntry.Text ?? "").Trim();
        var ph = string.IsNullOrWhiteSpace(PhoneEntryForm.Text) ? null : PhoneEntryForm.Text.Trim();
        var note = (NoteEntryForm.Text ?? "").Trim();
        var bk = string.IsNullOrWhiteSpace(BankEntry.Text) ? null : BankEntry.Text.Trim();
        var ac = string.IsNullOrWhiteSpace(AccountEntry.Text) ? null : AccountEntry.Text.Trim();
        var ho = string.IsNullOrWhiteSpace(HolderEntry.Text) ? null : HolderEntry.Text.Trim();

        if (string.IsNullOrWhiteSpace(name) || (string.IsNullOrEmpty(DigitsOnly(ph)) && string.IsNullOrEmpty(DigitsOnly(ac))))
        {
            StatusLabel.Text = "Nhập tên và SĐT hoặc số tài khoản.";
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.IsVisible = true;
            return;
        }

        var (ok, _, err) = await _api.AddRecipientAsync(name, ph, note, bk, ac, ho);
        if (ok)
        {
            NameEntry.Text = PhoneEntryForm.Text = BankEntry.Text = AccountEntry.Text = HolderEntry.Text = NoteEntryForm.Text = "";
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
