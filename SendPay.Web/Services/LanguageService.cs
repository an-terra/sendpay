namespace SendPay.Web.Services;

public class LanguageService
{
    public string Current { get; private set; } = "ja";
    public event Action? OnChanged;

    public string T(string key)
    {
        if (Translations.All.TryGetValue(Current, out var dict) && dict.TryGetValue(key, out var val))
            return val;
        if (Translations.All.TryGetValue("en", out var en) && en.TryGetValue(key, out var ev))
            return ev;
        return key;
    }

    /// <summary>D\u1ecbch key v\u00e0 thay c\u00e1c placeholder d\u1ea1ng {name} b\u1eb1ng <paramref name="args"/>.</summary>
    public string T(string key, IReadOnlyDictionary<string, object?>? args)
    {
        var template = T(key);
        if (args is null || args.Count == 0 || string.IsNullOrEmpty(template))
            return template;

        var sb = new System.Text.StringBuilder(template.Length + 16);
        for (var i = 0; i < template.Length; i++)
        {
            var ch = template[i];
            if (ch == '{')
            {
                var end = template.IndexOf('}', i + 1);
                if (end > i + 1)
                {
                    var name = template.Substring(i + 1, end - i - 1);
                    if (args.TryGetValue(name, out var value))
                    {
                        sb.Append(FormatValue(value));
                        i = end;
                        continue;
                    }
                }
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    private static string FormatValue(object? value)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        return value switch
        {
            null              => "",
            decimal d         => d.ToString("N0", inv),
            double dd         => dd.ToString("N0", inv),
            float f           => f.ToString("N0", inv),
            long l            => l.ToString("N0", inv),
            int i             => i.ToString("N0", inv),
            IFormattable ifmt => ifmt.ToString(null, inv),
            _                 => value.ToString() ?? ""
        };
    }

    public void Set(string lang)
    {
        if (Current == lang) return;
        Current = lang;
        OnChanged?.Invoke();
    }

    public static readonly (string Code, string Flag, string Label)[] Languages =
    [
        ("ja", "🇯🇵", "日本語"),
        ("vi", "🇻🇳", "Tiếng Việt"),
        ("en", "🇬🇧", "English"),
        ("ko", "🇰🇷", "한국어"),
        ("zh", "🇨🇳", "中文"),
    ];
}
