using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace SendPay.Api.Services;

public record CurrencyRate(
    string Code, string Flag, string Country,
    decimal Rate, string Change, bool Up);

public record ExchangeRateResponse(string Date, List<CurrencyRate> Rates);

public class RatesService(HttpClient http, IMemoryCache cache)
{
    private const string CacheKey = "exchange_rates";

    private static readonly string[] WantedCodes =
        ["vnd", "usd", "eur", "cny", "krw", "lkr", "npr", "php", "inr", "thb"];

    private static readonly Dictionary<string, (string Flag, string Country, string Display)> Meta = new()
    {
        ["vnd"] = ("🇻🇳", "Vietnam",       "VND"),
        ["usd"] = ("🇺🇸", "United States", "USD"),
        ["eur"] = ("🇪🇺", "Europe",        "EUR"),
        ["cny"] = ("🇨🇳", "China",         "CNY"),
        ["krw"] = ("🇰🇷", "South Korea",   "KRW"),
        ["lkr"] = ("🇱🇰", "Sri Lanka",     "LKR"),
        ["npr"] = ("🇳🇵", "Nepal",         "NPR"),
        ["php"] = ("🇵🇭", "Philippines",   "PHP"),
        ["inr"] = ("🇮🇳", "India",         "INR"),
        ["thb"] = ("🇹🇭", "Thailand",      "THB"),
    };

    public async Task<ExchangeRateResponse> GetRatesAsync()
    {
        if (cache.TryGetValue(CacheKey, out ExchangeRateResponse? cached) && cached != null)
            return cached;

        var (todayDate, todayRates) = await FetchAsync("latest");

        // Compare against the day before the returned "latest" date so we always get a real diff
        var prevDate = DateTime.TryParse(todayDate, out var td)
            ? td.AddDays(-1).ToString("yyyy-MM-dd")
            : DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd");

        var (_, yesterdayRates) = await FetchAsync(prevDate);

        var rates = new List<CurrencyRate>();
        foreach (var code in WantedCodes)
        {
            if (!todayRates.TryGetValue(code, out var rate) || rate == 0) continue;
            var (flag, country, display) = Meta.GetValueOrDefault(code);

            string change = "0.00";
            bool up = false;
            if (yesterdayRates.TryGetValue(code, out var yRate) && yRate > 0)
            {
                var pct = (rate - yRate) / yRate * 100m;
                change = (pct >= 0 ? "+" : "") + pct.ToString("F2");
                up = pct >= 0;
            }

            rates.Add(new CurrencyRate(display, flag, country, rate, change, up));
        }

        var result = new ExchangeRateResponse(todayDate, rates);
        cache.Set(CacheKey, result, TimeSpan.FromHours(6));
        return result;
    }

    private async Task<(string Date, Dictionary<string, decimal> Rates)> FetchAsync(string date)
    {
        var dict = new Dictionary<string, decimal>();
        try
        {
            var url  = $"https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@{date}/v1/currencies/jpy.json";
            var json = await http.GetStringAsync(url);
            using var doc  = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var dateStr = root.TryGetProperty("date", out var d) ? d.GetString() ?? date : date;

            if (root.TryGetProperty("jpy", out var jpy))
                foreach (var code in WantedCodes)
                    if (jpy.TryGetProperty(code, out var v))
                        dict[code] = v.GetDecimal();

            return (dateStr, dict);
        }
        catch
        {
            return (date, dict);
        }
    }
}
