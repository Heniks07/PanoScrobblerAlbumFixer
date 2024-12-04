using Newtonsoft.Json;

namespace PanoScrobblerAlbumFixer.API;

public class AlbumInfo
{
    [JsonProperty("album")] public required SimpleAlbum Album { get; set; }
}

public class SimpleAlbum
{
    [JsonProperty("playcount")]
    [JsonConverter(typeof(ParseStringConverter))]
    public long Playcount { get; set; }
}