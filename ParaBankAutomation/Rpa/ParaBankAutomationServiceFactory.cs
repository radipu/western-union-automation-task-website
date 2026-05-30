using ParaBankAutomation.Abstractions;

namespace ParaBankAutomation.Rpa;

public sealed class ParaBankAutomationServiceFactory : ICustomerAutomationServiceFactory
{
    public ICustomerAutomationService Create() => new ParaBankAutomationService();
}
