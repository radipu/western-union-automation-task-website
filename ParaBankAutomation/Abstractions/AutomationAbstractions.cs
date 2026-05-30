using ParaBankAutomation.Models;

namespace ParaBankAutomation.Abstractions;

public interface ICustomerSourceReader
{
    IReadOnlyList<CustomerProfile> ReadCustomers(string filePath);
}

public interface IExchangeRateProvider
{
    Task<decimal> GetUsdToEurRateAsync();
}

public interface IOperationReportWriter
{
    byte[] WriteReport(IReadOnlyList<OperationReportRow> rows, decimal exchangeRate);
}

public interface ICustomerValidationService
{
    CustomerValidationResult Validate(CustomerProfile customer);
}

public interface ICustomerAutomationService : IDisposable
{
    OperationReportRow ProcessCustomer(
        CustomerProfile customer,
        AutomationSettings settings,
        Action<string> logCallback);
}

public interface ICustomerAutomationServiceFactory
{
    ICustomerAutomationService Create();
}
