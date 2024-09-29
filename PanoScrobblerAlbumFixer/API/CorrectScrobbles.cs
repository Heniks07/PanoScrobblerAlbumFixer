namespace PanoScrobblerAlbumFixer.API;

public class CorrectScrobbles(string apiKey, string user)
{
    public RecentTracks GetRecentTracks()
    {
        var url =
            $"http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={user}&api_key={apiKey}&format=json";

        using var client = new HttpClient();
        var response = client.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
        var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        var recentTracks = RecentTracksJson.FromJson(result);
        return recentTracks.RecentTracks;
    }
}