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
