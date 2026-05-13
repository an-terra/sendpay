using SendPay.Mobile.Services;

namespace SendPay.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _api;

    public LoginPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    async void OnLoginClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        LoginBtn.IsEnabled = false;
        LoginBtn.Text = "Đang đăng nhập...";

        var (ok, data, err) = await _api.LoginAsync(EmailEntry.Text ?? "", PasswordEntry.Text ?? "");

        if (ok && data != null)
        {
            Preferences.Set("token", data.Token);
            Preferences.Set("fullName", data.FullName);
            await Shell.Current.GoToAsync("//dashboard");
        }
        else
        {
            ErrorLabel.Text = err;
            ErrorLabel.IsVisible = true;
            LoginBtn.IsEnabled = true;
            LoginBtn.Text = "Đăng nhập";
        }
    }

    async void OnRegisterTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("register");
}
