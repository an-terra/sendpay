using System.Globalization;
using System.Text.Json;

namespace SendPay.Api.Services;

/// <summary>JSON deterministic cho so khớp OTP (start vs confirm).</summary>
public static class OtpPayloadBuilder
{
    public static string TopUp(decimal amount)
    {
        var a = RoundMoney(amount).ToString(CultureInfo.InvariantCulture);
        return $$"""{"amount":{{a}},"purpose":"topup"}""";
    }

    public static string Transfer(string receiverPhone, decimal amount, string? note)
    {
        var phone = NormalizePhone(receiverPhone);
        var a = RoundMoney(amount).ToString(CultureInfo.InvariantCulture);
        var noteJson = JsonSerializer.Serialize(note?.Trim() ?? "");
        return $$"""{"amount":{{a}},"note":{{noteJson}},"purpose":"transfer","receiverPhone":"{{phone}}"}""";
    }

    private static decimal RoundMoney(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    public static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "";
        return new string(phone.Where(char.IsDigit).ToArray());
    }
}
