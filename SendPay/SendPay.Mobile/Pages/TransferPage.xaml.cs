using SendPay.Mobile.Services;

namespace SendPay.Mobile.Pages;

public partial class TransferPage : ContentPage
{
    private readonly ApiService _api;

    public TransferPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    async void OnTransferClicked(object sender, EventArgs e)
    {
        MsgLabel.IsVisible = false;
        if (!decimal.TryParse(AmountEntry.Text?.Replace(",", ""), out decimal amount))
        {
            ShowMsg("Số tiền không hợp lệ", "#dc2626");
            return;
        }

        TransferBtn.IsEnabled = false;
        TransferBtn.Text = "Đang xử lý...";

        var (ok, data, err) = await _api.TransferAsync(
            PhoneEntry.Text ?? "", amount, NoteEntry.Text ?? "");

        TransferBtn.IsEnabled = true;
        TransferBtn.Text = "Xác nhận chuyển tiền";

        if (ok && data != null)
        {
            ShowMsg($"✓ Đã chuyển {data.Amount:N0}₫ cho {data.ReceiverName} thành công!", "#16a34a");
            PhoneEntry.Text = AmountEntry.Text = NoteEntry.Text = "";
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
