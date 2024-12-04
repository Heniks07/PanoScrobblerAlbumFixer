using Newtonsoft.Json;

namespace PanoScrobblerAlbumFixer.API;

public class AlbumChecker(string apiKey)
{
    public long GetPlaycount(string albumName, string artistName)
    {
        var url =
            $"https://ws.audioscrobbler.com/2.0/?method=album.getinfo&api_key={apiKey}&artist={artistName}&album={albumName}&format=json";
        using var client = new HttpClient();
        var response = client.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
        var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        var albumInfo = JsonConvert.DeserializeObject<AlbumInfo>(result) ?? throw new JsonException("Album not found");
        return albumInfo.Album.Playcount;
    }
}