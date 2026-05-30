namespace ParaBankAutomation.Services;

public sealed class MoneyConverter
{
    private readonly decimal _usdToEurRate;

    public MoneyConverter(decimal usdToEurRate)
    {
        if (usdToEurRate <= 0) throw new ArgumentOutOfRangeException(nameof(usdToEurRate), "Exchange rate must be positive.");
        _usdToEurRate = usdToEurRate;
    }

    public decimal ToEur(decimal usd) => Math.Round(usd * _usdToEurRate, 2, MidpointRounding.AwayFromZero);

    public static decimal CalculateDownPayment(decimal amountUsd, decimal downPaymentRate) =>
        Math.Round(amountUsd * downPaymentRate, 2, MidpointRounding.AwayFromZero);
}
