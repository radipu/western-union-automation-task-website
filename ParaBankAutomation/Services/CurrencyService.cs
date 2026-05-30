using Newtonsoft.Json.Linq;
using ParaBankAutomation.Abstractions;

namespace ParaBankAutomation.Services;

public sealed class CurrencyService : IExchangeRateProvider
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<CurrencyService> _logger;
    private decimal? _cached;
    private const decimal Fallback = 0.92m;

    public CurrencyService(IHttpClientFactory http, ILogger<CurrencyService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<decimal> GetUsdToEurRateAsync()
    {
        if (_cached.HasValue) return _cached.Value;

        try
        {
            var client = _http.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var json = await client.GetStringAsync("https://api.frankfurter.app/latest?from=USD&to=EUR");
            var rate = JObject.Parse(json)["rates"]?["EUR"]?.Value<decimal>();
            if (rate > 0) { _cached = rate; return rate!.Value; }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live rate unavailable. Falling back to {Rate}.", Fallback);
        }

        _cached = Fallback;
        return Fallback;
    }
}
