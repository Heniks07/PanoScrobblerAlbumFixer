namespace PanoScrobblerAlbumFixer.API;

public class Unscrobble(User user)
{
    public void UnscrobbleTrack(Track track)
    {
        using var client = new HttpClient();

        client.DefaultRequestHeaders.Add("Cookie",
            $"sessionid={user.SessionId};" +
            $"csrftoken={user.CsrfToken};");
        client.DefaultRequestHeaders.Add("Referer", "https://www.last.fm/user/henikx");
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (X11; Linux x86_64; rv:132.0) Gecko/20100101 Firefox/132.0");


        var url = "https://www.last.fm/user/henikx/library/delete";
        var formData = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("csrfmiddlewaretoken",
                user.CsrfToken ?? throw new InvalidOperationException()),
            new KeyValuePair<string, string>("artist_name", track.Artist.Text),
            new KeyValuePair<string, string>("track_name", track.Name),
            new KeyValuePair<string, string>("timestamp",
                track.Date.Uts.ToString() ?? throw new InvalidOperationException()),
            new KeyValuePair<string, string>("ajax", "1")
        ]);

        var response = client.PostAsync(url, formData).ConfigureAwait(false).GetAwaiter().GetResult();
        var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        if (result.Contains("error"))
        {
            Console.WriteLine("Error while unscrobbling track\n" + result);
        }
    }
}