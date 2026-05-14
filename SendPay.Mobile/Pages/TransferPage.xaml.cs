using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SendPay.Mobile.Services;

namespace SendPay.Mobile.Pages;

[QueryProperty(nameof(RecipientIdQuery), "recipientId")]
[QueryProperty(nameof(PhoneQuery), "phone")]
public partial class TransferPage : ContentPage
{
    private readonly ApiService _api;
    private Guid? _verificationId;
    CancellationTokenSource? _phoneLookupCts;

    string _recipientIdQuery = "";
    public string RecipientIdQuery
    {
        get => _recipientIdQuery;
        set
        {
            _recipientIdQuery = value ?? "";
            _ = LoadFromRecipientAsync(_recipientIdQuery);
        }
    }

    public string PhoneQuery
    {
        set =>
            MainThread.BeginInvokeOnMainThread(() => PhoneEntry.Text = Uri.UnescapeDataString(value ?? ""));
    }

    public TransferPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    async Task LoadFromRecipientAsync(string raw)
    {
        if (!int.TryParse(raw, out var rid)) return;
        var rec = await _api.GetRecipientByIdAsync(rid);
        if (rec == null) return;
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            PhoneEntry.Text = rec.Phone ?? "";
            BankNameEntry.Text = rec.BankName ?? "";
            AccountEntry.Text = rec.AccountNumber ?? "";
            SwiftEntry.Text = rec.SwiftBic ?? "";
            NoteEntry.Text = rec.Note ?? "";
        });
        await DebouncedLookupAsync();
    }

    async void OnSendOtpClicked(object sender, EventArgs e)
    {
        MsgLabel.IsVisible = false;
        if (!decimal.TryParse(AmountEntry.Text?.Replace(",", ""), out decimal amount))
        {
            ShowMsg("Số tiền không hợp lệ", "#dc2626");
            return;
        }

        var phD = DigitsOnly(PhoneEntry.Text);
        var acK = AccountKey(AccountEntry.Text);
        if (phD.Length < 8 && acK.Length < 6)
        {
            ShowMsg("Nhập SĐT (≥8 số) hoặc STK (≥6 số).", "#dc2626");
            return;
        }

        var lookup = await _api.LookupTransferCounterpartyAsync(
            string.IsNullOrWhiteSpace(PhoneEntry.Text) ? null : PhoneEntry.Text.Trim(),
            string.IsNullOrWhiteSpace(AccountEntry.Text) ? null : AccountEntry.Text.Trim(),
            string.IsNullOrWhiteSpace(BankNameEntry.Text) ? null : BankNameEntry.Text.Trim());

        if (lookup?.IsSelf == true)
        {
            ShowMsg("Bạn không thể chuyển tiền cho chính mình.", "#dc2626");
            return;
        }

        if (lookup is not { Found: true, FullName: { } fn } || string.IsNullOrWhiteSpace(fn))
        {
            ShowMsg("Không tìm thấy người nhận trong danh bạ hoặc SendPay.", "#dc2626");
            return;
        }

        var eff = string.IsNullOrWhiteSpace(lookup.ResolvedPhone)
            ? PhoneEntry.Text?.Trim() ?? ""
            : lookup.ResolvedPhone.Trim();
        if (DigitsOnly(eff).Length < 8)
        {
            ShowMsg("Chuyển ví cần SĐT người nhận (≥8 số). Thêm SĐT vào danh bạ hoặc nhập SĐT SendPay.", "#dc2626");
            return;
        }

        SendOtpBtn.IsEnabled = false;
        SendOtpBtn.Text = "Đang gửi...";
        var (ok, data, err) = await _api.StartTransferVerificationAsync(eff, amount, NoteEntry.Text ?? "");
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

        var lookup = await _api.LookupTransferCounterpartyAsync(
            string.IsNullOrWhiteSpace(PhoneEntry.Text) ? null : PhoneEntry.Text.Trim(),
            string.IsNullOrWhiteSpace(AccountEntry.Text) ? null : AccountEntry.Text.Trim(),
            string.IsNullOrWhiteSpace(BankNameEntry.Text) ? null : BankNameEntry.Text.Trim());
        var eff = string.IsNullOrWhiteSpace(lookup?.ResolvedPhone)
            ? PhoneEntry.Text?.Trim() ?? ""
            : lookup!.ResolvedPhone!.Trim();
        if (DigitsOnly(eff).Length < 8)
        {
            ShowMsg("Thiếu SĐT người nhận hợp lệ.", "#dc2626");
            return;
        }

        TransferBtn.IsEnabled = false;
        TransferBtn.Text = "Đang xử lý...";

        var rb = string.IsNullOrWhiteSpace(BankNameEntry.Text) ? null : BankNameEntry.Text.Trim();
        var racct = string.IsNullOrWhiteSpace(AccountEntry.Text) ? null : AccountEntry.Text.Trim();
        var (ok, data, err) = await _api.TransferAsync(eff, amount, NoteEntry.Text ?? "", _verificationId.Value, otp, rb, racct);

        TransferBtn.IsEnabled = true;
        TransferBtn.Text = "2. Xác nhận chuyển tiền";

        if (ok && data != null)
        {
            ShowMsg($"✓ Đã chuyển {data.Amount:N0}₫ cho {data.ReceiverName} thành công!", "#16a34a");
            PhoneEntry.Text = AmountEntry.Text = NoteEntry.Text = "";
            BankNameEntry.Text = AccountEntry.Text = SwiftEntry.Text = "";
            OtpEntry.Text = "";
            ReceiverHintLabel.Text = "";
            ReceiverHintLabel.IsVisible = false;
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

    async void OnReceiverFieldTextChanged(object? sender, TextChangedEventArgs e) => await DebouncedLookupAsync();

    async Task DebouncedLookupAsync()
    {
        _phoneLookupCts?.Cancel();
        _phoneLookupCts?.Dispose();
        _phoneLookupCts = new CancellationTokenSource();
        var token = _phoneLookupCts.Token;
        try { await Task.Delay(400, token); }
        catch (TaskCanceledException) { return; }

        var phD = DigitsOnly(PhoneEntry.Text);
        var acK = AccountKey(AccountEntry.Text);
        if (phD.Length < 8 && acK.Length < 6)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ReceiverHintLabel.IsVisible = false;
                ReceiverHintLabel.Text = "";
            });
            return;
        }

        var res = await _api.LookupTransferCounterpartyAsync(
            string.IsNullOrWhiteSpace(PhoneEntry.Text) ? null : PhoneEntry.Text.Trim(),
            string.IsNullOrWhiteSpace(AccountEntry.Text) ? null : AccountEntry.Text.Trim(),
            string.IsNullOrWhiteSpace(BankNameEntry.Text) ? null : BankNameEntry.Text.Trim());

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ReceiverHintLabel.IsVisible = true;
            if (res?.IsSelf == true)
            {
                ReceiverHintLabel.TextColor = Color.FromArgb("#dc2626");
                ReceiverHintLabel.Text = "Bạn không thể chuyển tiền cho chính mình.";
            }
            else if (res is { Found: true, FullName: { } n } && !string.IsNullOrWhiteSpace(n))
            {
                ReceiverHintLabel.TextColor = Color.FromArgb("#16a34a");
                var bank = string.IsNullOrEmpty(res.BankDisplay) ? "" : $" · {res.BankDisplay}";
                ReceiverHintLabel.Text = $"Tài khoản nhận: {n}{bank}";
            }
            else
            {
                ReceiverHintLabel.TextColor = Color.FromArgb("#b45309");
                ReceiverHintLabel.Text = "Không tìm thấy trong danh bạ / SendPay.";
            }
        });
    }

    static string AccountKey(string? s) =>
        string.IsNullOrEmpty(s) ? "" : string.Concat(s.Where(char.IsLetterOrDigit)).ToUpperInvariant();

    static string DigitsOnly(string? s) =>
        string.IsNullOrEmpty(s) ? "" : new string(s.Where(char.IsDigit).ToArray());
}
