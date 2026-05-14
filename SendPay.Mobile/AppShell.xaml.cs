namespace SendPay.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("register", typeof(Pages.LoginPage));
        Routing.RegisterRoute("transfer", typeof(Pages.TransferPage));
        Routing.RegisterRoute("topup",    typeof(Pages.TopUpPage));
        Routing.RegisterRoute("recipients", typeof(Pages.RecipientsPage));
        Routing.RegisterRoute("history", typeof(Pages.HistoryPage));
        Routing.RegisterRoute("banksettings", typeof(Pages.JapanBankSettingsPage));
    }
}
