namespace PanoScrobblerAlbumFixer.API;

public class Unscrobble(Configuration config)
{
    private readonly User _user = config.User;

    public void UnscrobbleTrack(Track track)
    {
        using var client = new HttpClient();

        client.DefaultRequestHeaders.Add("Cookie",
            $"sessionid={_user.SessionId};" +
            $"csrftoken={_user.CsrfToken};");
        client.DefaultRequestHeaders.Add("Referer", $"https://www.last.fm/user/{_user.Name}");
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (X11; Linux x86_64; rv:132.0) Gecko/20100101 Firefox/132.0");


        var url = $"{config.Domain}/user/{_user.Name}/library/delete";
        var formData = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("csrfmiddlewaretoken",
                _user.CsrfToken ?? throw new InvalidOperationException()),
            new KeyValuePair<string, string>("artist_name", track.Artist.Text),
            new KeyValuePair<string, string>("track_name", track.Name),
            new KeyValuePair<string, string>("timestamp",
                track.Date.Uts.ToString() ?? throw new InvalidOperationException()),
            new KeyValuePair<string, string>("ajax", "1")
        ]);

        var response = client.PostAsync(url, formData).ConfigureAwait(false).GetAwaiter().GetResult();
        var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        if (!result.Contains("error")) return;
        File.WriteAllText("error.html", result);
        throw new HttpRequestException("Error occurred, check error.html in your working directory");
    }
}