using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;

namespace SendPay.Api.Services;

public class OtpDeliveryService(
    IConfiguration config,
    IWebHostEnvironment env,
    IHttpClientFactory httpFactory,
    ILogger<OtpDeliveryService> log) : IOtpDeliveryService
{
    public async Task<OtpNotifyOutcome> NotifyAsync(
        string email,
        string phone,
        string code,
        string actionDescription,
        CancellationToken cancellationToken = default)
    {
        var mode = ParseMode(config["Otp:DeliveryMode"] ?? "Log");
        var isDev = env.IsDevelopment();

        if (mode == OtpDeliveryMode.Log)
        {
            if (!isDev)
            {
                throw new InvalidOperationException(
                    "Production cần gửi OTP thật. Đặt Otp:DeliveryMode thành Email, Sms hoặc Both và cấu hình SMTP/Twilio (hoặc biến môi trường tương ứng).");
            }

            if (log.IsEnabled(LogLevel.Information))
                log.LogInformation("OTP (dev/log) user {Action}: {Code} — không gửi SMTP/SMS", actionDescription, code);
            return new OtpNotifyOutcome(null) { IsNoop = true };
        }

        var emailOk = false;
        var smsOk = false;

        if (mode is OtpDeliveryMode.Email or OtpDeliveryMode.Both)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Tài khoản chưa có email — không thể gửi OTP qua email.");

            await SendEmailAsync(email, code, actionDescription, cancellationToken);
            emailOk = true;
        }

        if (mode is OtpDeliveryMode.Sms or OtpDeliveryMode.Both)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new InvalidOperationException("Tài khoản chưa có số điện thoại — không thể gửi OTP qua SMS.");

            await SendTwilioSmsAsync(phone, code, actionDescription, cancellationToken);
            smsOk = true;
        }

        var msg = (emailOk, smsOk) switch
        {
            (true, false) => "Mã xác thực đã gửi tới email đăng ký. Vui lòng kiểm tra hộp thư (cả mục spam).",
            (false, true) => "Mã xác thực đã gửi qua SMS tới số điện thoại đăng ký.",
            (true, true)  => "Mã xác thực đã gửi qua email và SMS. Vui lòng kiểm tra cả hai.",
            _             => "Mã xác thực đã được gửi."
        };

        return new OtpNotifyOutcome(msg);
    }

    private async Task SendEmailAsync(string toEmail, string code, string action, CancellationToken ct)
    {
        var host = config["Otp:Email:Host"];
        var port = config.GetValue("Otp:Email:Port", 587);
        var user = config["Otp:Email:UserName"];
        var pass = config["Otp:Email:Password"];
        var from = config["Otp:Email:From"];
        var display = config["Otp:Email:FromDisplayName"] ?? "SendPay";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException(
                "Thiếu cấu hình Otp:Email (Host, From). Trên hosting đặt biến Otp__Email__Host, Otp__Email__From, …");
        }

        var enableSsl = config.GetValue("Otp:Email:EnableSsl", true);

        using var message = new MailMessage();
        message.From = new MailAddress(from, display);
        message.To.Add(toEmail);
        message.Subject = "SendPay — Mã xác thực giao dịch";
        message.Body =
            $"Mã OTP của bạn: {code}\n\n" +
            $"Giao dịch: {action}\n" +
            "Mã có hiệu lực 5 phút. Không chia sẻ mã này với bất kỳ ai.\n\n" +
            "— SendPay";
        message.IsBodyHtml = false;

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrEmpty(user))
            smtp.Credentials = new NetworkCredential(user, pass);

        await smtp.SendMailAsync(message, ct);
    }

    private async Task SendTwilioSmsAsync(string rawPhone, string code, string action, CancellationToken ct)
    {
        var sid = config["Otp:Sms:TwilioAccountSid"];
        var token = config["Otp:Sms:TwilioAuthToken"];
        var from = config["Otp:Sms:TwilioFrom"];

        if (string.IsNullOrWhiteSpace(sid) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException(
                "Thiếu Otp:Sms (TwilioAccountSid, TwilioAuthToken, TwilioFrom). Dùng Twilio hoặc đổi DeliveryMode sang Email.");
        }

        var toE164 = ToE164(rawPhone);
        var body =
            $"SendPay OTP: {code} ({action}). Hieu luc 5 phut.";

        var url = $"https://api.twilio.com/2010-04-01/Accounts/{sid}/Messages.json";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{sid}:{token}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = toE164,
            ["From"] = from,
            ["Body"] = body
        });

        var client = httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(25);
        using var res = await client.SendAsync(req, ct);
        var txt = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            log.LogWarning("Twilio lỗi {Status}: {Body}", (int)res.StatusCode, txt.Length > 500 ? txt[..500] : txt);
            throw new InvalidOperationException("Không gửi được SMS (Twilio). Kiểm tra số định dạng E.164 hoặc cấu hình.");
        }
    }

    private string ToE164(string rawPhone)
    {
        var cc = config["Otp:Sms:CountryCallingCode"] ?? "81";

        var trimmed = rawPhone.Trim();
        if (trimmed.StartsWith('+'))
        {
            var d = new string(trimmed.Where(char.IsDigit).ToArray());
            return "+" + d;
        }

        var digits = OtpPayloadBuilder.NormalizePhone(rawPhone);
        if (string.IsNullOrEmpty(digits))
            throw new InvalidOperationException("Số điện thoại không hợp lệ để gửi SMS.");

        if (digits.StartsWith(cc, StringComparison.Ordinal))
            return "+" + digits;

        if (digits[0] == '0')
            return "+" + cc + digits[1..];

        return "+" + cc + digits;
    }

    private static OtpDeliveryMode ParseMode(string? s) =>
        Enum.TryParse<OtpDeliveryMode>(s, ignoreCase: true, out var m) ? m : OtpDeliveryMode.Log;

    private enum OtpDeliveryMode
    {
        Log,
        Email,
        Sms,
        Both
    }
}
