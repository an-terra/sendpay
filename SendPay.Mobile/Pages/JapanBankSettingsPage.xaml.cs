using SendPay.Mobile.Services;

namespace SendPay.Mobile.Pages;

public partial class JapanBankSettingsPage : ContentPage
{
    private readonly ApiService _api;
    private string _fullName = "";
    private string _email = "";
    private string _phone = "";

    public JapanBankSettingsPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        MsgLabel.IsVisible = false;
        try
        {
            var p = await _api.GetProfileAsync();
            if (p == null) return;
            _fullName = p.FullName;
            _email = p.Email;
            _phone = p.Phone;
            BankNameEntry.Text = p.JapanBankName ?? "";
            BankUrlEntry.Text = p.JapanBankTopUpUrl ?? "";
        }
        catch
        {
            ShowMsg("Không tải được hồ sơ.", "#dc2626");
        }
    }

    async void OnSaveClicked(object sender, EventArgs e)
    {
        MsgLabel.IsVisible = false;
        var name = (BankNameEntry.Text ?? "").Trim();
        var url = (BankUrlEntry.Text ?? "").Trim();
        if (!string.IsNullOrEmpty(url) &&
            !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            ShowMsg("URL phải bắt đầu bằng http:// hoặc https://.", "#dc2626");
            return;
        }

        SaveBtn.IsEnabled = false;
        SaveBtn.Text = "Đang lưu...";
        var (ok, err) = await _api.UpdateProfileAsync(
            _fullName, _email, _phone,
            string.IsNullOrEmpty(name) ? null : name,
            string.IsNullOrEmpty(url) ? null : url);
        SaveBtn.IsEnabled = true;
        SaveBtn.Text = "Lưu";

        if (ok) ShowMsg("Đã lưu.", "#16a34a");
        else ShowMsg(string.IsNullOrWhiteSpace(err) ? "Lưu thất bại." : err, "#dc2626");
    }

    void ShowMsg(string msg, string color)
    {
        MsgLabel.Text = msg;
        MsgLabel.TextColor = Color.FromArgb(color);
        MsgLabel.IsVisible = true;
    }
}
