using Newtonsoft.Json;
using OpenQA.Selenium;

namespace PanoScrobblerAlbumFixer;

public class ParsableCookies
{
    public string cookieName { get; set; }
    public string cookieValue { get; set; }
    public string cookiePath { get; set; }
    public string cookieDomain { get; set; }
    public string sameSite { get; set; }
    public bool isHttpOnly { get; set; }
    public bool secure { get; set; }
    public DateTime? cookieExpiry { get; set; }

    [JsonConstructor]
    public ParsableCookies(string cookieName, string cookieValue, string cookiePath, string cookieDomain,
        string sameSite, bool isHttpOnly, bool secure, DateTime? cookieExpiry)
    {
        this.cookieName = cookieName;
        this.cookieValue = cookieValue;
        this.cookiePath = cookiePath;
        this.cookieDomain = cookieDomain;
        this.sameSite = sameSite;
        this.isHttpOnly = isHttpOnly;
        this.secure = secure;
        this.cookieExpiry = cookieExpiry;
    }

    public Cookie ToCookie()
    {
        return new Cookie(cookieName, cookieValue, cookieDomain, cookiePath, cookieExpiry);
    }
}