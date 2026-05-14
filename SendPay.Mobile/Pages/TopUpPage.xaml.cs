using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using SendPay.Mobile.Services;

namespace SendPay.Mobile.Pages;

public partial class TopUpPage : ContentPage
{
    private readonly ApiService _api;
    string? _japanTopUpUrl;

    public TopUpPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var p = await _api.GetProfileAsync();
            _japanTopUpUrl = p?.JapanBankTopUpUrl?.Trim();
            var name = (p?.JapanBankName ?? "").Trim();
            var has = !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(_japanTopUpUrl);
            JapanBankNameLabel.Text = name;
            JapanBankNameLabel.IsVisible = has;
            OpenJapanBankBtn.IsVisible = has;
            JapanBankNoneLabel.IsVisible = !has;
            GoBankSettingsBtn.IsVisible = !has;
        }
        catch
        {
            _japanTopUpUrl = null;
            JapanBankNameLabel.IsVisible = false;
            OpenJapanBankBtn.IsVisible = false;
            JapanBankNoneLabel.IsVisible = true;
            GoBankSettingsBtn.IsVisible = true;
        }
    }

    async void OnGoBankSettingsClicked(object sender, EventArgs e)
    {
        MsgLabel.IsVisible = false;
        await Shell.Current.GoToAsync("banksettings");
    }

    async void OnOpenJapanBankClicked(object sender, EventArgs e)
    {
        MsgLabel.IsVisible = false;
        if (string.IsNullOrWhiteSpace(_japanTopUpUrl) ||
            !Uri.TryCreate(_japanTopUpUrl, UriKind.Absolute, out var uri))
        {
            ShowMsg("Chưa có liên kết ngân hàng hợp lệ.", "#dc2626");
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(uri);
        }
        catch
        {
            ShowMsg("Không mở được trình duyệt / liên kết.", "#dc2626");
        }
    }

    async void OnTopUpClicked(object sender, EventArgs e)
    {
        MsgLabel.IsVisible = false;
        if (!decimal.TryParse(AmountEntry.Text?.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
        {
            ShowMsg("Số tiền không hợp lệ", "#dc2626");
            return;
        }

        TopUpBtn.IsEnabled = false;
        TopUpBtn.Text = "Đang xử lý...";

        var (ok, data, err) = await _api.TopUpAsync(amount);

        TopUpBtn.IsEnabled = true;
        TopUpBtn.Text = "Xác nhận nạp ví";

        if (ok && data != null)
        {
            if (string.Equals(data.Mode, "instant", StringComparison.OrdinalIgnoreCase) && data.Wallet != null)
            {
                RefHintLabel.IsVisible = false;
                ShowMsg($"✓ Nạp thành công. Số dư: ¥{data.Wallet.Balance:N0}", "#16a34a");
                AmountEntry.Text = "";
            }
            else
            {
                var exp = data.ExpiresAt?.ToUniversalTime().ToString("yyyy-MM-dd HH:mm") ?? "—";
                RefHintLabel.Text =
                    $"Nội dung CK (ghi đúng): {data.ReferenceCode}\nSố tiền: ¥{data.ExpectedAmount:N0}\nHết hạn UTC: {exp}";
                RefHintLabel.IsVisible = true;
                ShowMsg("Đã tạo lệnh nạp. Sau khi tiền vào và đối soát khớp, số dư sẽ tăng.", "#0f766e");
            }
        }
        else ShowMsg(err, "#dc2626");
    }

    void ShowMsg(string msg, string color)
    {
        MsgLabel.Text = msg;
        MsgLabel.TextColor = Color.FromArgb(color);
        MsgLabel.IsVisible = true;
    }
}
