using SendPay.Mobile.Services;

namespace SendPay.Mobile.Pages;

public class TransactionItem
{
    public string Icon        { get; set; } = "";
    public string Name        { get; set; } = "";
    public string Note        { get; set; } = "";
    public bool   HasNote     { get; set; }
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

    async Task LoadHistory()
    {
        var history = await _api.GetHistoryAsync();
        TxList.ItemsSource = history.Select(tx => new TransactionItem
        {
            Icon        = tx.Type == 1 ? "⬆️" : "↗️",
            Name        = tx.Type == 1 ? "Nạp tiền" : $"{tx.SenderName} → {tx.ReceiverName}",
            Note        = tx.Note,
            HasNote     = !string.IsNullOrEmpty(tx.Note) && tx.Type != 1,
            Date        = tx.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            AmountText  = $"{(tx.Type == 1 ? "+" : "-")}{tx.Amount:N0}₫",
            AmountColor = tx.Type == 1 ? "#22c55e" : "#ef4444"
        }).ToList();
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadHistory();
        Refresher.IsRefreshing = false;
    }
}
