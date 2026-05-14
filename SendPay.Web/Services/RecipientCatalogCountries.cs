namespace SendPay.Web.Services;

/// <summary>Quốc gia có danh mục ngân hàng cố định trên API.</summary>
public static class RecipientCatalogCountries
{
    public static readonly string[] Codes = ["VN", "LK", "NP", "PH"];

    public static bool UsesCatalog(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return false;
        return Codes.Contains(countryCode.Trim().ToUpperInvariant());
    }

    /// <summary>Dữ liệu cũ: CountryCode null + có Swift → coi là VN.</summary>
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
