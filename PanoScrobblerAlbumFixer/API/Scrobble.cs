using System.Text;
using static System.DateTimeOffset;

namespace PanoScrobblerAlbumFixer.API;

public class Scrobble(string apiKey, string secret)
{
    private const string Method = "track.scrobble";
    private const string ApiUrl = "https://ws.audioscrobbler.com/2.0/";

    public string ScrobbleSingleTrack(Track track, User user)
    {
        var signatureBase =
            $"api_key{apiKey}artist[0]{track.Artist.Text}method{Method}sk{user.Key}timestamp[0]{track.Date.Uts}track[0]{track.Name}{secret}";
        var apiSig = Cryptography.GetMd5Hash(signatureBase);


        //All the parameters are required to be in the post body
        var parameters = new Dictionary<string, string>
        {
            { "method", "track.scrobble" },
            { "artist[0]", track.Artist.Text },
            { "track[0]", track.Name },
            { "timestamp[0]", track.Date.Uts.ToString() },
            { "api_key", apiKey },
            { "sk", user.Key },
            { "format", "json" },
            { "api_sig", apiSig }
        };

        var content = new FormUrlEncodedContent(parameters);
        using var httpClient = new HttpClient();

        var response = httpClient.PostAsync(ApiUrl, content).ConfigureAwait(false).GetAwaiter().GetResult();
        return response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public string ScrobbleMultipleTracks(List<Track> tracks, User user)
    {
        var signatureBase = $"api_key{apiKey}method{Method}sk{user.Key}";

        var parameters = new Dictionary<string, string>
        {
            { "method", "track.scrobble" },
            { "api_key", apiKey },
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

        var apiSignature = GenerateApiSignature(parameters, secret);
        parameters.Add("format", "json");
        parameters.Add("api_sig", apiSignature);

        var content = new FormUrlEncodedContent(parameters);

        using var httpClient = new HttpClient();
        var response = httpClient.PostAsync(ApiUrl, content).ConfigureAwait(false).GetAwaiter().GetResult();
        return response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
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