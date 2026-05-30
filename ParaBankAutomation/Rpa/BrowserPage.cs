using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace ParaBankAutomation.Rpa;

[DebuggerStepThrough]
internal abstract class BrowserPage
{
    protected BrowserPage(IWebDriver driver)
    {
        Driver = driver;

        var waitSeconds = GetConfiguredWaitSeconds();

        Wait = new WebDriverWait(driver, TimeSpan.FromSeconds(waitSeconds))
        {
            PollingInterval = TimeSpan.FromMilliseconds(500)
        };
    }

    protected IWebDriver Driver { get; }
    protected WebDriverWait Wait { get; }

    protected IWebElement WaitFor(By locator)
    {
        return Wait.Until(d => GetVisibleEnabledElement(d, locator))!;
    }
    
    protected bool IsPresent(By locator)
    {
        try { return Driver.FindElements(locator).Count > 0; }
        catch (WebDriverException) { return false; }
    }

    protected string TryText(By locator)
    {
        try
        {
            var el = Driver.FindElements(locator).FirstOrDefault(e => e.Displayed);
            return el?.Text.Trim() ?? string.Empty;
        }
        catch (WebDriverException) { return string.Empty; }
    }

    protected void WaitForPageReady()
    {
        if (Driver is not IJavaScriptExecutor js) return;
        Wait.Until(_ =>
            string.Equals(js.ExecuteScript("return document.readyState")?.ToString(),
                "complete", StringComparison.OrdinalIgnoreCase));
    }

    protected void Type(By locator, string value)
    {
        Wait.Until(d =>
        {
            var el = GetVisibleEnabledElement(d, locator);
            if (el is null) return false;
            el.Clear();
            el.SendKeys(value ?? string.Empty);
            return true;
        });
    }

    protected void Click(By locator)
    {
        Wait.Until(d =>
        {
            var el = GetVisibleEnabledElement(d, locator);
            if (el is null) return false;
            ScrollIntoView(el);
            SafeClick(el);
            return true;
        });
        WaitForPageReady();
    }

    protected void SubmitForm(By submitLocator)
    {
        Wait.Until(d =>
        {
            var el = GetVisibleEnabledElement(d, submitLocator);
            if (el is null) return false;
            ScrollIntoView(el);
            SafeClick(el);
            return true;
        });
        WaitForPageReady();
    }

    protected string BodyText() => TryText(By.TagName("body"));

    private static int GetConfiguredWaitSeconds()
    {
        const int defaultSeconds = 180;
        const int minimumSeconds = 30;
        const int maximumSeconds = 600;

        var raw = Environment.GetEnvironmentVariable("PARABANK_WAIT_SECONDS");
        if (!int.TryParse(raw, out var seconds)) return defaultSeconds;

        return Math.Clamp(seconds, minimumSeconds, maximumSeconds);
    }

    #region Helpers

    private static IWebElement? GetVisibleEnabledElement(ISearchContext ctx, By locator)
    {
        var elements = ctx.FindElements(locator);
        foreach (var el in elements)
            if (el.Displayed && el.Enabled) return el;
        return null;
    }

    private void ScrollIntoView(IWebElement el)
    {
        if (Driver is IJavaScriptExecutor js)
            js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", el);
    }

    private void SafeClick(IWebElement el)
    {
        if (Driver is IJavaScriptExecutor js)
            js.ExecuteScript("arguments[0].click();", el);
        else
            el.Click();
    }

    #endregion
}
