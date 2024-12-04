using System.Text;
using Spectre.Console;
using static System.DateTimeOffset;

namespace PanoScrobblerAlbumFixer.API;

public class Scrobble(Configuration config)
{
    private readonly string _apiUrl = config.ScrobblerDomain;

    public void ScrobbleMultipleTracks(List<Track> tracks, User user)
    {
        var parameters = new Dictionary<string, string>
        {
            { "method", "track.scrobble" },
            { "api_key", config.ApiKey },
            { "sk", user.Key }
        };

        for (var i = 0; i < tracks.Count; i++)
        {
            if (tracks[i].Album != null && !string.IsNullOrEmpty(tracks[i].Album.Title))
            {
                parameters.Add($"album[{i}]", tracks[i].Album.Title);
            }

            parameters.Add($"artist[{i}]", tracks[i].Artist.Text);
            parameters.Add($"track[{i}]", tracks[i].Name);
            parameters.Add($"timestamp[{i}]",
                (tracks[i].Date.Uts + 60).ToString() ?? UtcNow.ToUnixTimeSeconds().ToString());
        }

        var apiSignature = GenerateApiSignature(parameters, config.ApiSecret);
        parameters.Add("format", "json");
        parameters.Add("api_sig", apiSignature);

        var content = new FormUrlEncodedContent(parameters);

        using var httpClient = new HttpClient();
        var response = httpClient.PostAsync(_apiUrl, content).ConfigureAwait(false).GetAwaiter().GetResult();
        var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        if (result.Contains("error")) { AnsiConsole.MarkupLine("[bold red]Error: {0}[/]", result); }
    }

    private static string GenerateApiSignature(Dictionary<string, string> parameters, string apiSecret)
    {
        // Sort the parameters in ASCII order (manual sorting based on key)
        var sortedParams = parameters.OrderBy(p => p.Key, StringComparer.Ordinal);

        var signatureBuilder = new StringBuilder();
        foreach (var param in sortedParams) signatureBuilder.Append(param.Key).Append(param.Value);

        // Append the API secret to the end
        signatureBuilder.Append(apiSecret);

        return Cryptography.GetMd5Hash(signatureBuilder.ToString());
    }
}