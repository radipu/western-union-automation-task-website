using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace ParaBankAutomation.Rpa.Flows;

internal sealed class OpenAccountFlow : BrowserPage
{
    public OpenAccountFlow(IWebDriver driver) : base(driver) { }

    public string OpenNewAccount(string accountType)
    {
        Click(By.LinkText("Open New Account"));

        // Wait until both dropdowns are populated by the page's JavaScript
        Wait.Until(_ => IsPresent(By.Id("type")) && IsPresent(By.Id("fromAccountId")));

        var typeDropdown = new SelectElement(WaitFor(By.Id("type")));
        var normalised = string.IsNullOrWhiteSpace(accountType)
            ? "CHECKING"
            : accountType.Trim().ToUpperInvariant();

        var match = typeDropdown.Options.FirstOrDefault(o =>
            string.Equals(o.Text.Trim(), normalised, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(o.GetAttribute("value")?.Trim(), normalised, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
            typeDropdown.SelectByText(match.Text.Trim());

        // Ensure the from-account dropdown has loaded at least one option
        Wait.Until(_ => new SelectElement(WaitFor(By.Id("fromAccountId"))).Options.Count > 0);

        SubmitForm(By.XPath("//input[contains(translate(@value, 'abcdefghijklmnopqrstuvwxyz', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'), 'OPEN NEW ACCOUNT')] | //button[contains(translate(normalize-space(.), 'abcdefghijklmnopqrstuvwxyz', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'), 'OPEN NEW ACCOUNT')]"));

        // Wait for the confirmation containing the new account id
        Wait.Until(_ =>
        {
            var id = TryText(By.Id("newAccountId"));
            var body = BodyText();
            return !string.IsNullOrWhiteSpace(id) ||
                   body.Contains("Account Opened", StringComparison.OrdinalIgnoreCase) ||
                   body.Contains("Congratulations", StringComparison.OrdinalIgnoreCase);
        });

        var newId = TryText(By.Id("newAccountId"));
        if (!string.IsNullOrWhiteSpace(newId)) return newId;

        // Fallback: ParaBank sometimes renders the number as an anchor only
        var links = Driver.FindElements(By.CssSelector("a[href*='activity.htm?id=']"));
        return links.LastOrDefault()?.Text.Trim() ?? string.Empty;
    }
}
