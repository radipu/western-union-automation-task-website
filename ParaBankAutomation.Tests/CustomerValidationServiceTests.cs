using ParaBankAutomation.Models;
using ParaBankAutomation.Services;
using Xunit;

namespace ParaBankAutomation.Tests;

public sealed class CustomerValidationServiceTests
{
    [Fact]
    public void Validate_MissingMandatoryFields_ReturnsErrors()
    {
        var validator = new CustomerValidationService();
        var customer = new CustomerProfile
        {
            LastName = "SyntheticLastName",
            AccountType = "Checking"
        };

        var result = validator.Validate(customer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("First name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("Username", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_InvalidDobAndZeroDeposit_AreWarningsNotBlockingErrors()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var validator = new CustomerValidationService();
        var customer = new CustomerProfile
        {
            FirstName = $"First{unique}",
            LastName = $"Last{unique}",
            Username = $"user_{unique}",
            Password = "SyntheticPassword!",
            AccountType = "Savings",
            InitialDepositUsd = 0m,
            DobRaw = "31/02/1987",
            DateOfBirth = null
        };

        var result = validator.Validate(customer);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains(result.Warnings, w => w.Contains("DOB", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, w => w.Contains("zero", StringComparison.OrdinalIgnoreCase));
    }
}
