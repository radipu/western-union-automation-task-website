using ParaBankAutomation.Abstractions;
using ParaBankAutomation.Models;

namespace ParaBankAutomation.Services;

public sealed class CustomerValidationService : ICustomerValidationService
{
    private static readonly HashSet<string> SupportedAccountTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "CHECKING",
        "SAVINGS"
    };

    public CustomerValidationResult Validate(CustomerProfile customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var result = new CustomerValidationResult();

        Require(customer.FirstName, "First name is required.", result);
        Require(customer.Username, "Username is required.", result);
        Require(customer.Password, "Password is required.", result);

        if (string.IsNullOrWhiteSpace(customer.AccountType))
            result.AddWarning("Account type was blank; CHECKING will be used by ParaBank.");
        else if (!SupportedAccountTypes.Contains(customer.AccountType.Trim()))
            result.AddWarning($"Unsupported account type '{customer.AccountType}'; ParaBank may fall back to the default option.");

        if (customer.InitialDepositUsd < 0)
            result.AddWarning("Initial deposit is negative; report values may need business review.");
        else if (customer.InitialDepositUsd == 0)
            result.AddWarning("Initial deposit is blank or zero; automation will continue but this should be reviewed.");

        if (!string.IsNullOrWhiteSpace(customer.DobRaw) && customer.DateOfBirth is null)
            result.AddWarning($"DOB '{customer.DobRaw}' could not be parsed; raw value will be kept in the report.");

        if (!string.IsNullOrWhiteSpace(customer.Cvv) && customer.Cvv.Length is < 3 or > 4)
            result.AddWarning("CVV length looks unusual.");

        return result;
    }

    private static void Require(string value, string message, CustomerValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(value)) result.AddError(message);
    }
}
