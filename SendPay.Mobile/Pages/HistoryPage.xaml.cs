using System.Linq;
using SendPay.Mobile.Services;

namespace SendPay.Mobile.Pages;

public class TransactionItem
{
    public string Icon        { get; set; } = "";
    public string Name        { get; set; } = "";
    public string Note        { get; set; } = "";
    public bool   HasNote     { get; set; }
    public string BankLine    { get; set; } = "";
    public bool   HasBank     { get; set; }
    public string Date        { get; set; } = "";
    public string AmountText  { get; set; } = "";
    public string AmountColor { get; set; } = "#22c55e";
}

public partial class HistoryPage : ContentPage
{
    private readonly ApiService _api;

    public HistoryPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistory();
    }

    static string AccountKey(string? s) =>
        string.IsNullOrEmpty(s) ? "" : string.Concat(s.Where(char.IsLetterOrDigit)).ToUpperInvariant();

    static string FormatBankLine(string? bankName, string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(bankName) && string.IsNullOrWhiteSpace(accountNumber)) return "";
        var bn = string.IsNullOrWhiteSpace(bankName) ? "—" : bankName.Trim();
        var key = AccountKey(accountNumber);
        var tail = string.IsNullOrEmpty(key) ? "" : (key.Length <= 4 ? key : key[^4..]);
        return string.IsNullOrEmpty(tail) ? bn : $"{bn} · ****{tail}";
    }

    async Task LoadHistory()
    {
        var history = await _api.GetHistoryAsync();
        TxList.ItemsSource = history.Select(tx =>
        {
            var bankLine = tx.Type == 0 ? FormatBankLine(tx.ReceiverBankName, tx.ReceiverAccountNumber) : "";
            return new TransactionItem
            {
                Icon        = tx.Type == 1 ? "⬆️" : "↗️",
                Name        = tx.Type == 1 ? "Nạp tiền" : $"{tx.SenderName} → {tx.ReceiverName}",
                Note        = tx.Note,
                HasNote     = !string.IsNullOrEmpty(tx.Note) && tx.Type != 1,
                BankLine    = bankLine,
                HasBank     = !string.IsNullOrEmpty(bankLine),
                Date        = tx.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                AmountText  = $"{(tx.Type == 1 ? "+" : "-")}{tx.Amount:N0}₫",
                AmountColor = tx.Type == 1 ? "#22c55e" : "#ef4444"
            };
        }).ToList();
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadHistory();
        Refresher.IsRefreshing = false;
    }
}
