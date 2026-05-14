using System.Globalization;
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
    List<RecipientResponse> _recipients = [];
    bool _skipNextSuggestUpdate;

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { _recipients = await _api.GetRecipientsAsync(); }
        catch { _recipients = []; }
    }

    async Task LoadFromRecipientAsync(string raw)
    {
        if (!int.TryParse(raw, out var rid)) return;
        var rec = await _api.GetRecipientByIdAsync(rid);
        if (rec == null) return;
        _skipNextSuggestUpdate = true;
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            PhoneEntry.Text = rec.Phone ?? "";
            BankNameEntry.Text = rec.BankName ?? "";
            AccountEntry.Text = rec.AccountNumber ?? "";
            HolderEntry.Text = rec.AccountHolderName ?? "";
            SwiftEntry.Text = rec.SwiftBic ?? "";
            NoteEntry.Text = rec.Note ?? "";
            SuggestBorder.IsVisible = false;
            SuggestStack.Children.Clear();
        });
        await DebouncedLookupAsync();
    }

    async void OnSendOtpClicked(object sender, EventArgs e)
    {
        MsgLabel.IsVisible = false;
        if (!decimal.TryParse(AmountEntry.Text?.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
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
            OtpHintLabel.Text = $"Mã OTP (giả lập / dev): {data.DebugOtp}";
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

        if (!decimal.TryParse(AmountEntry.Text?.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
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
            BankNameEntry.Text = AccountEntry.Text = SwiftEntry.Text = HolderEntry.Text = "";
            OtpEntry.Text = "";
            ReceiverHintLabel.Text = "";
            ReceiverHintLabel.IsVisible = false;
            _verificationId = null;
            OtpHintLabel.IsVisible = false;
            SuggestBorder.IsVisible = false;
            SuggestStack.Children.Clear();
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
        ReceiverLookupDto? res = null;
        if (phD.Length >= 8 || acK.Length >= 6)
        {
            res = await _api.LookupTransferCounterpartyAsync(
                string.IsNullOrWhiteSpace(PhoneEntry.Text) ? null : PhoneEntry.Text.Trim(),
                string.IsNullOrWhiteSpace(AccountEntry.Text) ? null : AccountEntry.Text.Trim(),
                string.IsNullOrWhiteSpace(BankNameEntry.Text) ? null : BankNameEntry.Text.Trim());
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (phD.Length < 8 && acK.Length < 6)
            {
                ReceiverHintLabel.IsVisible = false;
                ReceiverHintLabel.Text = "";
            }
            else
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
            }

            UpdateRecipientSuggestButtons();
        });
    }

    void UpdateRecipientSuggestButtons()
    {
        if (_skipNextSuggestUpdate)
        {
            _skipNextSuggestUpdate = false;
            return;
        }

        var terms = new List<string>();
        void T(string? s)
        {
            s = (s ?? "").Trim();
            if (s.Length >= 2) terms.Add(s);
        }

        T(PhoneEntry.Text);
        T(BankNameEntry.Text);
        T(AccountEntry.Text);
        T(HolderEntry.Text);
        T(SwiftEntry.Text);

        SuggestStack.Children.Clear();
        if (terms.Count == 0)
        {
            SuggestBorder.IsVisible = false;
            return;
        }

        var list = _recipients
            .Where(r => terms.Exists(t =>
                (r.Name?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Phone?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.BankName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.AccountNumber?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.AccountHolderName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.SwiftBic?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false)))
            .Take(8)
            .ToList();

        if (list.Count == 0)
        {
            SuggestBorder.IsVisible = false;
            return;
        }

        SuggestBorder.IsVisible = true;
        foreach (var r in list)
        {
            var sub = new List<string>();
            if (!string.IsNullOrWhiteSpace(r.Phone)) sub.Add(r.Phone!);
            var bankAcct = string.Join(" · ", new[] { r.BankName, r.AccountNumber }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (!string.IsNullOrEmpty(bankAcct)) sub.Add(bankAcct);

            var b = new Button
            {
                Text = sub.Count == 0 ? r.Name : $"{r.Name}\n{string.Join(" · ", sub)}",
                BackgroundColor = Colors.White,
                TextColor = Colors.Black,
                BorderColor = Color.FromArgb("#e2e8f0"),
                BorderWidth = 1,
                CornerRadius = 8,
                Padding = new Thickness(10, 8),
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap
            };
            var captured = r;
            b.Clicked += async (_, _) => await ApplyRecipientFromBookAsync(captured);
            SuggestStack.Children.Add(b);
        }
    }

    async Task ApplyRecipientFromBookAsync(RecipientResponse r)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            PhoneEntry.Text = r.Phone ?? "";
            BankNameEntry.Text = r.BankName ?? "";
            AccountEntry.Text = r.AccountNumber ?? "";
            HolderEntry.Text = r.AccountHolderName ?? "";
            SwiftEntry.Text = r.SwiftBic ?? "";
            if (string.IsNullOrEmpty(NoteEntry.Text))
                NoteEntry.Text = r.Note ?? "";
            SuggestBorder.IsVisible = false;
            SuggestStack.Children.Clear();
        });
        _skipNextSuggestUpdate = true;
        await DebouncedLookupAsync();
    }

    static string AccountKey(string? s) =>
        string.IsNullOrEmpty(s) ? "" : string.Concat(s.Where(char.IsLetterOrDigit)).ToUpperInvariant();

    static string DigitsOnly(string? s) =>
        string.IsNullOrEmpty(s) ? "" : new string(s.Where(char.IsDigit).ToArray());
}
