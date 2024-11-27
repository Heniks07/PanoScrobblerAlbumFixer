using Newtonsoft.Json;
using Spectre.Console;

namespace PanoScrobblerAlbumFixer.API;

public class Authentication
{
    private readonly string _apiKey;
    private readonly string _secret;
    private const string Method = "auth.getToken";

    public string Token { get; private set; }

    public Authentication(string apiKey, string secret)
    {
        _apiKey = apiKey;
        _secret = secret;
    }

    public void GetToken()
    {
        var signatureBase = $"api_key{_apiKey}method{Method}{_secret}";
        var apiSig = Cryptography.GetMd5Hash(signatureBase);
        var url = $"https://ws.audioscrobbler.com/2.0/?method={Method}&api_key={_apiKey}&api_sig={apiSig}&format=json";

        using var client = new HttpClient();
        var response = client.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
        var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        string token = JsonConvert.DeserializeObject<dynamic>(result)?.token ??
                       "Error: No token found: " + result;
        Token = token;
    }

    public string AuthUrl()
    {
        return $"http://www.last.fm/api/auth/?api_key={_apiKey}&token={Token}";
    }

    public User GetSession()
    {
        var method = "auth.getSession";

        // Construct the API signature
        var signatureBase = $"api_key{_apiKey}method{method}token{Token}{_secret}";
        var apiSig = Cryptography.GetMd5Hash(signatureBase);

        // Call auth.getSession API
        var url =
            $"https://ws.audioscrobbler.com/2.0/?method={method}&api_key={_apiKey}&token={Token}&api_sig={apiSig}&format=json";

        using var client = new HttpClient();
        var response = client.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
        var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        var session = JsonConvert.DeserializeObject<dynamic>(result)?.session;
        var user = session != null
            ? new User
            {
                Name = session.name,
                Key = session.key,
                Subscriber = session.subscriber
            }
            : null;
        if (user?.Name == null)
        {
            AnsiConsole.MarkupLine("[bold red]Error: No username found: {0}[/]", result);


            user = new User
            {
                Name = AnsiConsole.Prompt(new TextPrompt<string>(
                           "Enter your last.fm username (note that there might have gone something wrong in the authentication Process. If you want to be sure rerun the application. If the problem keeps persisting create an issue on https://github.com/Heniks07/PanoScrobblerAlbumFixer/issues):")) ??
                       throw new ArgumentNullException("No username entered")
            };
        }

        return user;
    }
}