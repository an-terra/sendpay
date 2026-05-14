namespace SendPay.Mobile.Services;

/// <summary>Quốc gia có danh mục ngân hàng cố định trên API.</summary>
public static class RecipientCatalogCountries
{
    public static readonly string[] Codes = ["VN", "LK", "NP", "PH"];

    public static readonly string[] PickerLabels =
    [
        "Việt Nam",
        "Sri Lanka",
        "Nepal",
        "Philippines",
        "Khác",
    ];

    public static bool UsesCatalog(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return false;
        return Codes.Contains(countryCode.Trim().ToUpperInvariant());
    }

    public static string CodeFromPickerIndex(int index) =>
        index >= 0 && index < Codes.Length ? Codes[index] : "OTHER";

    public static int PickerIndexFromCode(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return PickerLabels.Length - 1;
        var c = countryCode.Trim().ToUpperInvariant();
        for (var i = 0; i < Codes.Length; i++)
        {
            if (Codes[i] == c) return i;
        }

        return PickerLabels.Length - 1;
    }

    public static string FormCountryFromRecipient(RecipientResponse r)
    {
        if (!string.IsNullOrEmpty(r.CountryCode))
        {
            var c = r.CountryCode.Trim().ToUpperInvariant();
            if (UsesCatalog(c)) return c;
            return "OTHER";
        }

        if (!string.IsNullOrEmpty(r.SwiftBic)) return "VN";
        return "OTHER";
    }
}
