using OpenQA.Selenium;
using ParaBankAutomation.Models;

namespace ParaBankAutomation.Rpa.Flows;

internal sealed class LoginCustomerFlow : BrowserPage
{
    private readonly string _homeUrl;

    public LoginCustomerFlow(IWebDriver driver, string homeUrl) : base(driver)
    {
        _homeUrl = homeUrl;
    }

    public bool Login(CustomerProfile customer)
    {
        Driver.Navigate().GoToUrl(_homeUrl);
        WaitForPageReady();

        Type(By.Name("username"), customer.Username);
        Type(By.Name("password"), customer.Password);
        SubmitForm(By.XPath("//input[@type='submit' and @value='Log In']"));

        var body = BodyText();
        return body.Contains("Account Services", StringComparison.OrdinalIgnoreCase) ||
               body.Contains("Open New Account", StringComparison.OrdinalIgnoreCase) ||
               body.Contains("Welcome", StringComparison.OrdinalIgnoreCase);
    }
}
