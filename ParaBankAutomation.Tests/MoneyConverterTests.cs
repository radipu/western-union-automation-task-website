using ParaBankAutomation.Services;
using Xunit;

namespace ParaBankAutomation.Tests;

public sealed class MoneyConverterTests
{
    [Fact]
    public void ToEur_RoundsToTwoDecimals()
    {
        var converter = new MoneyConverter(0.92345m);

        var result = converter.ToEur(123.456m);

        Assert.Equal(114.01m, result);
    }

    [Fact]
    public void CalculateDownPayment_UsesConfiguredRate()
    {
        var initialDeposit = 500m;
        var configuredDownPaymentRate = 0.20m;

        var result = MoneyConverter.CalculateDownPayment(initialDeposit, configuredDownPaymentRate);

        Assert.Equal(100m, result);
    }

    [Fact]
    public void Constructor_RejectsInvalidExchangeRate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MoneyConverter(0m));
    }
}
