using OpenQA.Selenium;
using ParaBankAutomation.Models;

namespace ParaBankAutomation.Rpa.Flows;

internal sealed class RegisterCustomerFlow : BrowserPage
{
    private readonly string _homeUrl;

    public RegisterCustomerFlow(IWebDriver driver, string homeUrl) : base(driver)
    {
        _homeUrl = homeUrl;
    }

    public string Register(CustomerProfile customer)
    {
        EnsureLoggedOut();

        Click(By.LinkText("Register"));

        Type(By.Id("customer.firstName"), customer.FirstName);
        Type(By.Id("customer.lastName"), customer.LastName);
        Type(By.Id("customer.address.street"),
            string.IsNullOrWhiteSpace(customer.Address) ? "N/A" : customer.Address);
        Type(By.Id("customer.address.city"), customer.City);
        Type(By.Id("customer.address.state"), customer.State);
        Type(By.Id("customer.address.zipCode"), customer.ZipCode);
        Type(By.Id("customer.phoneNumber"), customer.PhoneNumber);
        Type(By.Id("customer.ssn"), customer.Ssn);
        Type(By.Id("customer.username"), customer.Username);
        Type(By.Id("customer.password"), customer.Password);
        Type(By.Id("repeatedPassword"), customer.Password);

        SubmitForm(By.XPath("//input[@type='submit' and @value='Register']"));

        var body = BodyText();

        if (body.Contains("Your account was created successfully", StringComparison.OrdinalIgnoreCase))
            return "Registered";

        if (body.Contains("This username already exists", StringComparison.OrdinalIgnoreCase))
            return "AlreadyExists";

        if (body.Contains("required", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("error", StringComparison.OrdinalIgnoreCase))
            return "Registration failed: ParaBank returned validation errors.";

        return "Registration could not be confirmed from the page response.";
    }

    private void EnsureLoggedOut()
    {
        if (IsPresent(By.LinkText("Log Out")))
            Click(By.LinkText("Log Out"));

        Driver.Navigate().GoToUrl(_homeUrl);
        WaitForPageReady();
    }
}
