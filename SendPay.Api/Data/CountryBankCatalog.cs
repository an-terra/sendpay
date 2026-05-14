using System.Diagnostics.CodeAnalysis;

namespace SendPay.Api.Data;

/// <summary>Danh sách ngân hàng phổ biến theo quốc gia — mã SWIFT gán nội bộ khi khớp catalog.</summary>
public static class CountryBankCatalog
{
    public sealed record Entry(string Name, string Swift);

    /// <summary>Mã ISO có catalog cố định (không gồm OTHER).</summary>
    public static IReadOnlyList<string> CatalogCountryCodes { get; } =
        ["VN", "LK", "NP", "PH"];

    private static readonly Dictionary<string, IReadOnlyList<Entry>> BanksByCountry = new(StringComparer.OrdinalIgnoreCase)
    {
        ["VN"] =
        [
            new("Ngân hàng Ngoại thương Việt Nam (Vietcombank)", "BVBVVNVX"),
            new("Ngân hàng Đầu tư và Phát triển Việt Nam (BIDV)", "BIDVVNVX"),
            new("Ngân hàng Công thương Việt Nam (VietinBank)", "ICBVVNVX"),
            new("Ngân hàng Nông nghiệp và Phát triển Nông thôn Việt Nam (Agribank)", "VBAAVNVX"),
            new("Ngân hàng Kỹ thương Việt Nam (Techcombank)", "VTCBVNVX"),
            new("Ngân hàng Á Châu (ACB)", "ASCBVNVX"),
            new("Ngân hàng Quân đội (MB Bank)", "MSCBVNVX"),
            new("Ngân hàng Tiên Phong (TPBank)", "TPBKVNVX"),
            new("Ngân hàng Việt Nam Thịnh Vượng (VPBank)", "VPBKVNVX"),
            new("Ngân hàng Sài Gòn Thương Tín (Sacombank)", "SACOVNVX"),
            new("Ngân hàng Phát triển Thành phố Hồ Chí Minh (HDBank)", "HDBCVNVX"),
            new("Ngân hàng Hàng Hải Việt Nam (MSB)", "MCOBVNVX"),
            new("Ngân hàng Sài Gòn — Hà Nội (SHB)", "SHBAVNVX"),
            new("Ngân hàng Phương Đông (OCB)", "ORCOVNVX"),
            new("Ngân hàng Quốc tế (VIB)", "VNIBVNVX"),
            new("Ngân hàng Đông Nam Á (SeABank)", "SEAVVNVX"),
            new("Ngân hàng Nam Á (Nam A Bank)", "NAMAVNVX"),
            new("Ngân hàng Xuất nhập khẩu Việt Nam (Eximbank)", "EBVIVNVX"),
            new("Ngân hàng Bưu điện Liên Việt (LienVietPostBank / LPBank)", "LPBKVNVX"),
            new("Ngân hàng Bảo Việt (BVBank)", "BVBAVNVX"),
            new("Ngân hàng Kiên Long (KienlongBank)", "KLBKVNVX"),
            new("Ngân hàng An Bình (ABBANK)", "ABBKVNVX"),
            new("Ngân hàng Bản Việt (VietCapital Bank)", "VCBCVNVX"),
            new("Ngân hàng Quốc dân (NCB)", "NCBVVNVX"),
            new("Ngân hàng Xăng dầu Petrolimex (PG Bank)", "PGBLVNVX"),
            new("Ngân hàng Việt Nam Thương tín (VietBank)", "VNTTVNVX"),
            new("Ngân hàng Đại chúng Việt Nam (PVcomBank)", "WBWFVNVX"),
            new("Ngân hàng TNHH MTV Public Việt Nam (Public Bank)", "VIDPVNVX"),
            new("Ngân hàng United Overseas — Chi nhánh TP.HCM (UOB)", "UOVBVNVX"),
            new("Ngân hàng TNHH MTV Shinhan Việt Nam (Shinhan Bank)", "SHBKVNVX"),
            new("Ngân hàng Woori Việt Nam (Woori Bank)", "HVBKVNVX"),
            new("Citibank Việt Nam", "CITIVNVX"),
            new("HSBC Việt Nam (HSBC Bank)", "HSBCVNVX"),
            new("Ngân hàng TNHH MTV Standard Chartered Việt Nam", "SCBLVNVX"),
            new("Ngân hàng CIMB Việt Nam (CIMB)", "CIBBVNVX"),
            new("Ngân hàng Hong Leong Việt Nam (HLBVN)", "HLBBVNVX"),
        ],
        ["LK"] =
        [
            new("Bank of Ceylon", "BCEYLKLX"),
            new("People's Bank", "PSBKLKLX"),
            new("Commercial Bank of Ceylon", "CCEYLKLX"),
            new("Sampath Bank", "BSAMLKLX"),
            new("Hatton National Bank", "HNBLKLKX"),
            new("Nations Trust Bank", "NTBCLKLX"),
            new("DFCC Bank", "DFCCLKLX"),
            new("Seylan Bank", "SEYBLKLX"),
            new("Pan Asia Banking Corporation", "PABSLKLX"),
            new("Union Bank of Colombo", "UBCLLKLC"),
            new("Amana Bank", "AMBLILKL"),
            new("National Savings Bank", "NSBALKLX"),
            new("Regional Development Bank", "RDBLLKLX"),
        ],
        ["NP"] =
        [
            new("Nepal Investment Bank", "NIBLNPKT"),
            new("NIC Asia Bank", "NICNPKA"),
            new("NMB Bank", "NMBBNPKA"),
            new("Global IME Bank", "GLBBNPKA"),
            new("Siddhartha Bank", "SIDDNPKA"),
            new("Nepal SBI Bank", "NSBINPKA"),
            new("Himalayan Bank", "HIMANPKA"),
            new("Agricultural Development Bank", "ADBLNPKA"),
            new("Everest Bank", "EVBLNPKA"),
            new("Sanima Bank", "SNMANPKA"),
            new("Prime Commercial Bank", "PCBLNPKA"),
            new("Kumari Bank", "KMBLNPKA"),
        ],
        ["PH"] =
        [
            new("Banco de Oro (BDO)", "BNORPHMM"),
            new("Bank of the Philippine Islands (BPI)", "BOPIPHMM"),
            new("Metropolitan Bank & Trust (Metrobank)", "MBTCPHMM"),
            new("Land Bank of the Philippines", "TLBPPHMM"),
            new("Security Bank", "SETCPHMM"),
            new("Rizal Commercial Banking (RCBC)", "RCBCPHMM"),
            new("Union Bank of the Philippines", "UBPHPHMM"),
            new("Philippine National Bank (PNB)", "PNBMPHMM"),
            new("China Banking Corporation", "CHBKPHMM"),
            new("East West Banking Corporation", "EWBCPHMM"),
            new("Philippine Bank of Communications", "CPHIPHMM"),
            new("Philippine Veterans Bank", "PHVBPHMM"),
            new("Development Bank of the Philippines", "DBPHPHMM"),
        ],
    };

    public static bool IsCatalogCountry(string? countryCode) =>
        !string.IsNullOrWhiteSpace(countryCode) &&
        BanksByCountry.ContainsKey(countryCode.Trim());

    /// <summary>Chuẩn hoá mã quốc gia: một trong catalog hoặc OTHER.</summary>
    public static string NormalizeCountry(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return "OTHER";
        var c = countryCode.Trim().ToUpperInvariant();
        return BanksByCountry.ContainsKey(c) ? c : "OTHER";
    }

    public static IReadOnlyList<Entry> GetBanks(string countryCode)
    {
        var c = (countryCode ?? "").Trim().ToUpperInvariant();
        return BanksByCountry.TryGetValue(c, out var list) ? list : [];
    }

    public static bool TryGetSwiftByBankName(string? countryCode, string? bankName,
        [NotNullWhen(true)] out string? swift)
    {
        swift = null;
        var c = NormalizeCountry(countryCode);
        if (c == "OTHER" || string.IsNullOrWhiteSpace(bankName)) return false;
        if (!BanksByCountry.TryGetValue(c, out var list)) return false;
        var t = bankName.Trim();
        foreach (var e in list)
        {
            if (string.Equals(e.Name, t, StringComparison.OrdinalIgnoreCase))
            {
                swift = e.Swift;
                return true;
            }
        }

        return false;
    }

    public static string? CanonicalBankName(string? countryCode, string? bankName)
    {
        var c = NormalizeCountry(countryCode);
        if (c == "OTHER" || string.IsNullOrWhiteSpace(bankName)) return null;
        if (!BanksByCountry.TryGetValue(c, out var list)) return null;
        var t = bankName.Trim();
        foreach (var e in list)
        {
            if (string.Equals(e.Name, t, StringComparison.OrdinalIgnoreCase))
                return e.Name;
        }

        return null;
    }
}
