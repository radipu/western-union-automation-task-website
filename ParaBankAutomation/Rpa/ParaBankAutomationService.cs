using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ParaBankAutomation.Abstractions;
using ParaBankAutomation.Models;
using ParaBankAutomation.Services;
using ParaBankAutomation.Rpa.Flows;

namespace ParaBankAutomation.Rpa;

[DebuggerStepThrough]
public sealed class ParaBankAutomationService : ICustomerAutomationService
{
    private IWebDriver? _driver;
    private bool _disposed;

    public OperationReportRow ProcessCustomer(
        CustomerProfile customer,
        AutomationSettings settings,
        Action<string> logCallback)
    {
        var driver = GetDriver(settings.RunBrowserHeadless);
        var calculator = new MoneyConverter(settings.UsdToEurRate);
        var downPaymentUsd = MoneyConverter.CalculateDownPayment(customer.InitialDepositUsd, settings.DownPaymentRate);
        var stage = "Starting browser session";

        try
        {
            var registerFlow = new RegisterCustomerFlow(driver, settings.ParaBankUrl);
            var loginFlow = new LoginCustomerFlow(driver, settings.ParaBankUrl);
            var accountFlow = new OpenAccountFlow(driver);
            var loanFlow = new LoanRequestFlow(driver);

            stage = "Navigating to ParaBank";
            logCallback($"[{customer.Username}] Opening ParaBank home page…");
            driver.Navigate().GoToUrl(settings.ParaBankUrl);

            stage = "Registering customer";
            logCallback($"[{customer.Username}] Registering account…");
            var registrationStatus = registerFlow.Register(customer);
            var notes = string.Empty;

            if (registrationStatus == "AlreadyExists")
            {
                notes = "Username already existed in ParaBank – logged in with provided credentials.";
                logCallback($"[{customer.Username}] Username exists, logging in…");
                stage = "Logging in existing customer";
                if (!loginFlow.Login(customer))
                    return BuildRow(customer, settings, calculator, downPaymentUsd,
                        "Not opened", "No", "Not requested", "Automation Failed",
                        "Username already existed and login failed.");
            }
            else if (registrationStatus != "Registered")
            {
                return BuildRow(customer, settings, calculator, downPaymentUsd,
                    "Not opened", "No", "Not requested", "Automation Failed", registrationStatus);
            }

            stage = "Opening new bank account";
            logCallback($"[{customer.Username}] Opening new {customer.AccountType} account…");
            var accountNumber = accountFlow.OpenNewAccount(customer.AccountType);

            if (string.IsNullOrWhiteSpace(accountNumber))
                return BuildRow(customer, settings, calculator, downPaymentUsd,
                    "Not opened", "No", "Not requested", "Automation Failed",
                    "Account was created but the account number could not be captured.");

            logCallback($"[{customer.Username}] Account #{accountNumber} opened.");

            stage = "Requesting loan";
            logCallback($"[{customer.Username}] Requesting ${settings.LoanAmountUsd:N0} loan…");
            var loanStatus = loanFlow.RequestLoan(settings.LoanAmountUsd, downPaymentUsd, accountNumber);
            logCallback($"[{customer.Username}] Loan result: {loanStatus}");

            stage = "Logging out";
            LogOut(driver);
            logCallback($"[{customer.Username}] Logged out. Done.");

            return BuildRow(customer, settings, calculator, downPaymentUsd,
                accountNumber, "Yes", loanStatus, "Completed", notes);
        }
        catch (WebDriverException ex)
        {
            TryRecoverBrowser(driver, settings.ParaBankUrl);
            var msg = CleanError(ex.Message);
            return BuildRow(customer, settings, calculator, downPaymentUsd,
                "Not opened", "No", "Not requested", "Automation Failed", $"{stage}: {msg}");
        }
        catch (InvalidOperationException ex)
        {
            TryRecoverBrowser(driver, settings.ParaBankUrl);
            return BuildRow(customer, settings, calculator, downPaymentUsd,
                "Not opened", "No", "Not requested", "Automation Failed", $"{stage}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _driver?.Quit(); }
        finally { _driver?.Dispose(); _driver = null; }
    }

    private IWebDriver GetDriver(bool headless)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_driver is not null) return _driver;

        var opts = new ChromeOptions();
        opts.AddArgument("--start-maximized");
        opts.AddArgument("--disable-notifications");
        opts.AddArgument("--disable-popup-blocking");
        opts.AddArgument("--disable-search-engine-choice-screen");

        if (headless)
        {
            opts.AddArgument("--headless=new");
            opts.AddArgument("--window-size=1400,900");
        }

        _driver = new ChromeDriver(opts);

        var timeout = TimeSpan.FromSeconds(GetConfiguredTimeoutSeconds());
        _driver.Manage().Timeouts().PageLoad = timeout;
        _driver.Manage().Timeouts().AsynchronousJavaScript = timeout;

        return _driver;
    }

    private static int GetConfiguredTimeoutSeconds()
    {
        const int defaultSeconds = 180;
        const int minimumSeconds = 30;
        const int maximumSeconds = 600;

        var raw = Environment.GetEnvironmentVariable("PARABANK_WAIT_SECONDS");
        if (!int.TryParse(raw, out var seconds)) return defaultSeconds;

        return Math.Clamp(seconds, minimumSeconds, maximumSeconds);
    }

    private static void LogOut(IWebDriver driver)
    {
        var links = driver.FindElements(By.LinkText("Log Out"));
        if (links.Count > 0) links[0].Click();
    }

    private static void TryRecoverBrowser(IWebDriver driver, string homeUrl)
    {
        try { LogOut(driver); driver.Navigate().GoToUrl(homeUrl); }
        catch (WebDriverException) { /* best-effort recovery */ }
    }

    private static string CleanError(string msg)
    {
        var first = msg.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(first) ? "Browser automation error." : first.Trim();
    }

    private static OperationReportRow BuildRow(
        CustomerProfile c, AutomationSettings s, MoneyConverter calc,
        decimal downUsd, string account, string loanReq, string loanStatus,
        string automationStatus, string notes)
    {
        return new OperationReportRow
        {
            RowNumber = c.RowNumber,
            CustomerName = c.FullName,
            Username = c.Username,
            AccountType = c.AccountType,
            InitialDepositUsd = c.InitialDepositUsd,
            InitialDepositEur = calc.ToEur(c.InitialDepositUsd),
            LoanAmountUsd = s.LoanAmountUsd,
            LoanAmountEur = calc.ToEur(s.LoanAmountUsd),
            DownPaymentUsd = downUsd,
            DownPaymentEur = calc.ToEur(downUsd),
            OpenedAccountNumber = string.IsNullOrWhiteSpace(account) ? "Not opened" : account,
            LoanRequested = loanReq,
            LoanStatus = loanStatus,
            DateOfBirth = c.DateOfBirth?.ToString("yyyy-MM-dd") ?? c.DobRaw,
            DebitCardNumber = c.DebitCardNumber,
            Cvv = c.Cvv,
            AutomationStatus = automationStatus,
            Notes = notes
        };
    }
}
