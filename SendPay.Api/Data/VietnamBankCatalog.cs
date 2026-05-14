using System.Diagnostics.CodeAnalysis;

namespace SendPay.Api.Data;

/// <summary>Danh sách ngân hàng Việt Nam phổ biến — mã SWIFT dùng nội bộ, không hiển thị cho user.</summary>
public static class VietnamBankCatalog
{
    public sealed record Entry(string Name, string Swift);

    public static IReadOnlyList<Entry> All { get; } =
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
    ];

    public static bool TryGetSwiftByBankName(string? bankName, [NotNullWhen(true)] out string? swift)
    {
        swift = null;
        if (string.IsNullOrWhiteSpace(bankName)) return false;
        var t = bankName.Trim();
        foreach (var e in All)
        {
            if (string.Equals(e.Name, t, StringComparison.OrdinalIgnoreCase))
            {
                swift = e.Swift;
                return true;
            }
        }

        return false;
    }

    public static string? CanonicalBankName(string? bankName)
    {
        if (string.IsNullOrWhiteSpace(bankName)) return null;
        var t = bankName.Trim();
        foreach (var e in All)
        {
            if (string.Equals(e.Name, t, StringComparison.OrdinalIgnoreCase))
                return e.Name;
        }

        return null;
    }
}
