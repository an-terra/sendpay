using System.Linq;
using SendPay.Mobile.Services;

namespace SendPay.Mobile.Pages;

public partial class TransferPage : ContentPage
{
    private readonly ApiService _api;
    private Guid? _verificationId;

    public TransferPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    async void OnSendOtpClicked(object sender, EventArgs e)
    {
        MsgLabel.IsVisible = false;
        if (!decimal.TryParse(AmountEntry.Text?.Replace(",", ""), out decimal amount))
        {
            ShowMsg("Số tiền không hợp lệ", "#dc2626");
            return;
        }

        SendOtpBtn.IsEnabled = false;
        SendOtpBtn.Text = "Đang gửi...";
        var (ok, data, err) = await _api.StartTransferVerificationAsync(
            PhoneEntry.Text ?? "", amount, NoteEntry.Text ?? "");
        SendOtpBtn.IsEnabled = true;
        SendOtpBtn.Text = "1. Gửi mã OTP";

        if (!ok || data is null)
        {
            _verificationId = null;
            OtpHintLabel.IsVisible = false;
            ShowMsg(err, "#dc2626");
            return;
        }

        _verificationId = data.VerificationId;
        if (!string.IsNullOrEmpty(data.DebugOtp))
        {
            OtpHintLabel.Text = $"Mã OTP (dev): {data.DebugOtp}";
            OtpHintLabel.IsVisible = true;
        }
        else
        {
            OtpHintLabel.Text = "Kiểm tra SMS/email số đăng ký.";
            OtpHintLabel.IsVisible = true;
        }

        ShowMsg("Đã gửi OTP. Nhập mã rồi bấm xác nhận.", "#0f766e");
    }

    async void OnTransferClicked(object sender, EventArgs e)
    {
        MsgLabel.IsVisible = false;
        if (_verificationId is null)
        {
            ShowMsg("Bấm \"Gửi mã OTP\" trước.", "#dc2626");
            return;
        }

        var otp = (OtpEntry.Text ?? "").Trim();
        if (otp.Length != 6 || !otp.All(char.IsDigit))
        {
            ShowMsg("Nhập đúng 6 chữ số OTP.", "#dc2626");
            return;
        }

        if (!decimal.TryParse(AmountEntry.Text?.Replace(",", ""), out decimal amount))
        {
            ShowMsg("Số tiền không hợp lệ", "#dc2626");
            return;
        }

        TransferBtn.IsEnabled = false;
        TransferBtn.Text = "Đang xử lý...";

        var (ok, data, err) = await _api.TransferAsync(
            PhoneEntry.Text ?? "", amount, NoteEntry.Text ?? "", _verificationId.Value, otp);

        TransferBtn.IsEnabled = true;
        TransferBtn.Text = "2. Xác nhận chuyển tiền";

        if (ok && data != null)
        {
            ShowMsg($"✓ Đã chuyển {data.Amount:N0}₫ cho {data.ReceiverName} thành công!", "#16a34a");
            PhoneEntry.Text = AmountEntry.Text = NoteEntry.Text = "";
            OtpEntry.Text = "";
            _verificationId = null;
            OtpHintLabel.IsVisible = false;
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
