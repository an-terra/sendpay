using SendPay.Mobile.Services;

namespace SendPay.Mobile.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly ApiService _api;

    public DashboardPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadData();
    }

    async Task LoadData()
    {
        GreetingLabel.Text = $"Xin chào, {Preferences.Get("fullName", "bạn")}!";

        var wallet = await _api.GetWalletAsync();
        if (wallet != null)
        {
            BalanceLabel.Text = $"{wallet.Balance:N0} ₫";
            PhoneLabel.Text = wallet.Phone;
        }

        var history = await _api.GetHistoryAsync();
        RecentList.ItemsSource = history.Take(5).Select(tx => new HistoryPage.TransactionItem
        {
            Icon        = tx.Type == 1 ? "⬆️" : "↗️",
            Name        = tx.Type == 1 ? "Nạp tiền" : $"→ {tx.ReceiverName}",
            Date        = tx.CreatedAt.ToLocalTime().ToString("dd/MM HH:mm"),
            AmountText  = $"{(tx.Type == 1 ? "+" : "-")}{tx.Amount:N0}₫",
            AmountColor = tx.Type == 1 ? "#22c55e" : "#ef4444"
        }).ToList();
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadData();
        Refresher.IsRefreshing = false;
    }

    async void OnLogoutClicked(object sender, EventArgs e)
    {
        Preferences.Remove("token");
        Preferences.Remove("fullName");
        await Shell.Current.GoToAsync("//login");
    }

    async void OnTopUpTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("topup");

    async void OnTransferTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("transfer");

    async void OnHistoryTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("history");
}
