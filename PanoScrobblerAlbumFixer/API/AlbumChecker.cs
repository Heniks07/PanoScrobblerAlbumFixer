using Newtonsoft.Json;

namespace PanoScrobblerAlbumFixer.API;

public class AlbumChecker(string apiKey)
{
    public long GetPlaycount(string AlbumName, string ArtistName)
    {
        var url =
            $"https://ws.audioscrobbler.com/2.0/?method=album.getinfo&api_key={apiKey}&artist={ArtistName}&album={AlbumName}&format=json";
        using var client = new HttpClient();
        var response = client.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
        var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        return JsonConvert.DeserializeObject<AlbumInfo>(result).Album.Playcount;
    }
}