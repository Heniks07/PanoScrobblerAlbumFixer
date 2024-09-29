namespace PanoScrobblerAlbumFixer.API;

public class TrackChecker(string apiKey)
{
    private string _apiKey = apiKey;

    public List<Track> GetWrongTracks(List<Track> tracks)
    {
        var wrongTracks = new List<Track>();
        foreach (var track in tracks)
        {
            var url =
                $"http://ws.audioscrobbler.com/2.0/?method=track.getInfo&api_key={apiKey}&artist={track.Artist.Text}&track={track.Name}&format=json";
            using var client = new HttpClient();
            var response = client.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
            var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var trackinfo = TrackInfo.FromJson(result).Track;

            if (trackinfo?.Album == null)
                continue;

            if (trackinfo.Album.Title != track.Album.Title && trackinfo.Album.Title != track.Album.Text)
                wrongTracks.Add(track);
        }

        return wrongTracks;
    }
}