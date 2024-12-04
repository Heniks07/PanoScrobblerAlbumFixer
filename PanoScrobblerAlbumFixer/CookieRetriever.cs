using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Spectre.Console;

namespace PanoScrobblerAlbumFixer;

public class CookieRetriever
{
    private readonly WebDriver _driver;
    private readonly Configuration _config;

    public CookieRetriever(Configuration config)
    {
        _config = config;

        var options = new FirefoxOptions();
        var service = FirefoxDriverService.CreateDefaultService();
#if RELEASE
        options.LogLevel = FirefoxDriverLogLevel.Fatal;
        options.SetPreference("javascript.options.showInConsole", false);
        options.SetPreference("dom.report_all_js_exceptions", false);
        options.SetPreference("browser.dom.window.dump.enabled", false);
        options.SetPreference("extensions.logging.enabled", false);
        service.LogLevel = FirefoxDriverLogLevel.Fatal;
        service.SuppressInitialDiagnosticInformation = true;
#endif


        _driver = new FirefoxDriver(service, options);
    }


    public void Login()
    {
        var userName = _config.User.Name;
        var password = _config.User.Password;

        _driver.Navigate().GoToUrl($"{_config.Domain}/login");
        _driver.FindElement(By.Id("id_username_or_email")).SendKeys(userName);
        _driver.FindElement(By.Id("id_password")).SendKeys(password);
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var loginButton =
            wait.Until(ExpectedConditions.ElementIsVisible(
                By.CssSelector("button[name='submit']")));

        AnsiConsole.WriteLine("Accept the cookies and press enter");
        Console.ReadLine();

        loginButton.Click();

        var cookies = _driver.Manage().Cookies.AllCookies;

        _config.User.SessionId = cookies.First(x => x.Name == "sessionid").Value;
        _config.User.CsrfToken = cookies.First(x => x.Name == "csrftoken").Value;

        _driver.Close();
        _driver.Dispose();
    }
}