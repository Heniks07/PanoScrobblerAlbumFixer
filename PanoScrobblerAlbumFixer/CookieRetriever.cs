using Newtonsoft.Json;
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

    private const string CookiePath = "cookies.json";

    public void Login(bool closeBrowser = false)
    {
        var userName = _config.User.Name;
        var password = _config.User.Password;
        if (File.Exists(CookiePath))
        {
            _driver.Navigate().GoToUrl($"https://www.last.fm/user/{userName}/library");
            RetrieveCookies();
            if (!closeBrowser)
                _driver.Navigate().Refresh();
        }
        else
        {
            _driver.Navigate().GoToUrl("https://www.last.fm/login");
            _driver.FindElement(By.Id("id_username_or_email")).SendKeys(userName);
            _driver.FindElement(By.Id("id_password")).SendKeys(password);
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            var loginButon =
                wait.Until(ExpectedConditions.ElementIsVisible(
                    By.CssSelector("button[name='submit']")));

            AnsiConsole.WriteLine("Accept the cookies and press enter");
            Console.ReadLine();

            loginButon.Click();

            StoreCookies();
        }

        if (!closeBrowser) return;
        _driver.Close();
        _driver.Dispose();
    }

    private void StoreCookies()
    {
        var cookies = _driver.Manage().Cookies.AllCookies;
        var parsableCookies = cookies.Select(c =>
            new ParsableCookies(c.Name, c.Value, c.Path, c.Domain, c.SameSite, c.IsHttpOnly, c.Secure, c.Expiry));
        File.WriteAllText(CookiePath, JsonConvert.SerializeObject(parsableCookies));
        var parsableCookiesEnumerable = parsableCookies.ToList();
        _config.User.SessionId =
            parsableCookiesEnumerable.FirstOrDefault(x => x.cookieName == "sessionid")?.cookieValue;
        _config.User.CsrfToken =
            parsableCookiesEnumerable.FirstOrDefault(x => x.cookieName == "csrftoken")?.cookieValue;
    }

    private void RetrieveCookies()
    {
        // Retrieve cookies
        var cookies =
            JsonConvert.DeserializeObject<List<ParsableCookies>>(File.ReadAllText(CookiePath));
        if (cookies == null) return;
        foreach (var cookie in cookies) _driver.Manage().Cookies.AddCookie(cookie.ToCookie());

        _config.User.SessionId = cookies.FirstOrDefault(x => x.cookieName == "sessionid")?.cookieValue;
        _config.User.CsrfToken = cookies.FirstOrDefault(x => x.cookieName == "csrftoken")?.cookieValue;
    }
}