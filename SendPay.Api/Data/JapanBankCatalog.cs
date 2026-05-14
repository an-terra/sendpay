namespace SendPay.Api.Data;

/// <summary>Danh sách ngân hàng Nhật phục vụ liên kết tài khoản (mock demo).</summary>
public static class JapanBankCatalog
{
    public sealed record Entry(
        string Code,
        string NameJa,
        string NameEn,
        string Kana,
        string Aliases,
        string Emoji
    );

    private static readonly IReadOnlyList<Entry> Banks =
    [
        new("MUFG",      "三菱UFJ銀行",        "MUFG Bank",                    "ミツビシユーエフジェイ", "mufg mitsubishi ufj 三菱 ufj",        "🏦"),
        new("SMBC",      "三井住友銀行",       "Sumitomo Mitsui Banking",     "ミツイスミトモ",       "smbc sumitomo mitsui 三井住友",       "🏦"),
        new("MIZUHO",    "みずほ銀行",         "Mizuho Bank",                  "ミズホ",               "mizuho みずほ",                        "🏦"),
        new("RESONA",    "りそな銀行",         "Resona Bank",                  "リソナ",               "resona りそな",                        "🏦"),
        new("YUCHO",     "ゆうちょ銀行",       "Japan Post Bank",              "ユウチョ",             "yucho yuucho japan post ゆうちょ 郵便", "📮"),
        new("RAKUTEN",   "楽天銀行",           "Rakuten Bank",                 "ラクテン",             "rakuten 楽天",                         "🟥"),
        new("PAYPAY",    "PayPay銀行",         "PayPay Bank",                  "ペイペイ",             "paypay ペイペイ",                      "💳"),
        new("SBISHINSEI","SBI新生銀行",        "SBI Shinsei Bank",             "エスビーアイシンセイ",  "sbi shinsei sbi新生 shinsei 新生",     "🏛"),
        new("AEON",      "イオン銀行",         "AEON Bank",                    "イオン",               "aeon イオン",                          "🛒"),
        new("SONY",      "ソニー銀行",         "Sony Bank",                    "ソニー",               "sony ソニー",                          "🎮"),
        new("SEVEN",     "セブン銀行",         "Seven Bank",                   "セブン",               "seven 7 セブン 711",                   "🏪"),
        new("LAWSON",    "ローソン銀行",       "Lawson Bank",                  "ローソン",             "lawson ローソン",                      "🏪"),
        new("SBISUMISHIN","住信SBIネット銀行", "SBI Sumishin Net Bank",        "ジュウシンエスビーアイ", "sbi sumishin net 住信",                "💻"),
        new("GMO",       "GMOあおぞらネット銀行","GMO Aozora Net Bank",        "ジーエムオーアオゾラ", "gmo aozora あおぞら",                  "💻"),
        new("SHIZUOKA",  "静岡銀行",           "The Shizuoka Bank",            "シズオカ",             "shizuoka 静岡",                        "🏯"),
        new("CHIBA",     "千葉銀行",           "The Chiba Bank",               "チバ",                 "chiba 千葉",                           "🏯"),
        new("FUKUOKA",   "福岡銀行",           "The Bank of Fukuoka",          "フクオカ",             "fukuoka 福岡",                         "🏯"),
        new("HOKKAIDO",  "北海道銀行",         "The Hokkaido Bank",            "ホッカイドウ",         "hokkaido 北海道",                      "🏔"),
        new("NISHINIPPON","西日本シティ銀行",  "The Nishi-Nippon City Bank",   "ニシニッポンシティ",   "nishi nippon city 西日本",             "🏯"),
        new("KYOTO",     "京都銀行",           "The Bank of Kyoto",            "キョウト",             "kyoto 京都",                           "⛩"),
        new("HIROSHIMA", "広島銀行",           "The Hiroshima Bank",           "ヒロシマ",             "hiroshima 広島",                       "🏯"),
        new("YOKOHAMA",  "横浜銀行",           "The Bank of Yokohama",         "ヨコハマ",             "yokohama 横浜",                        "⚓"),
        new("MINATO",    "ミナト銀行",         "The Minato Bank",              "ミナト",               "minato ミナト",                        "🏯"),
        new("KIRABOSHI", "きらぼし銀行",       "Kiraboshi Bank",               "キラボシ",             "kiraboshi きらぼし",                   "✨"),
    ];

    public static IReadOnlyList<Entry> All => Banks;

    public static Entry? FindByCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var c = code.Trim().ToUpperInvariant();
        return Banks.FirstOrDefault(b => string.Equals(b.Code, c, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<Entry> Search(string? query, int max = 25)
    {
        if (max <= 0) max = 25;
        if (string.IsNullOrWhiteSpace(query))
            return Banks.Take(max);

        var q = query.Trim().ToLowerInvariant();
        return Banks
            .Where(b =>
                b.Code.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                b.NameJa.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                b.NameEn.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                b.Kana.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                b.Aliases.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(max)
            .ToList();
    }
}
