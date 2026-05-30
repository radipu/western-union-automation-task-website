using System.Globalization;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace ParaBankAutomation.Rpa.Flows;

internal sealed class LoanRequestFlow : BrowserPage
{
    public LoanRequestFlow(IWebDriver driver) : base(driver) { }

    public string RequestLoan(decimal loanAmountUsd, decimal downPaymentUsd, string newAccountNumber)
    {
        Click(By.LinkText("Request Loan"));

        Type(By.Id("amount"), loanAmountUsd.ToString("0.00", CultureInfo.InvariantCulture));
        Type(By.Id("downPayment"), downPaymentUsd.ToString("0.00", CultureInfo.InvariantCulture));

        // Wait for the from-account dropdown to load
        Wait.Until(_ => new SelectElement(WaitFor(By.Id("fromAccountId"))).Options.Count > 0);

        var fromSel = new SelectElement(WaitFor(By.Id("fromAccountId")));

        // Try to select the newly created account; fall back to first option
        var matchingOption = fromSel.Options.FirstOrDefault(o =>
            string.Equals(o.Text.Trim(), newAccountNumber, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(o.GetAttribute("value")?.Trim(), newAccountNumber, StringComparison.OrdinalIgnoreCase));

        if (matchingOption is not null)
            fromSel.SelectByText(matchingOption.Text.Trim());

        SubmitForm(By.XPath("//input[contains(translate(@value, 'abcdefghijklmnopqrstuvwxyz', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'), 'APPLY NOW')] | //button[contains(translate(normalize-space(.), 'abcdefghijklmnopqrstuvwxyz', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'), 'APPLY NOW')]"));

        // Wait for the result page. ParaBank uses any of these indicators
        Wait.Until(_ =>
        {
            var body = BodyText();
            return body.Contains("Loan Request Processed", StringComparison.OrdinalIgnoreCase) ||
                   body.Contains("Approved", StringComparison.OrdinalIgnoreCase) ||
                   body.Contains("Denied", StringComparison.OrdinalIgnoreCase) ||
                   body.Contains("Error", StringComparison.OrdinalIgnoreCase);
        });

        var page = BodyText();

        if (page.Contains("Approved", StringComparison.OrdinalIgnoreCase))
        {
            var loanAccount = TryText(By.Id("newAccountId"));
            return string.IsNullOrWhiteSpace(loanAccount)
                ? "Approved - loan request confirmed"
                : $"Approved - loan account {loanAccount}";
        }

        if (page.Contains("Denied", StringComparison.OrdinalIgnoreCase))
            return "Denied - loan request not approved";

        if (page.Contains("Loan Request Processed", StringComparison.OrdinalIgnoreCase))
            return "Submitted - loan request processed";

        return "Submitted - status page displayed";
    }
}
