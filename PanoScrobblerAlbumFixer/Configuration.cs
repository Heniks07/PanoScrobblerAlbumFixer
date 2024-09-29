using Newtonsoft.Json;
using PanoScrobblerAlbumFixer.API;

namespace PanoScrobblerAlbumFixer;

public class Configuration
{
    [JsonProperty("apiKey")] public string ApiKey { get; set; }
    [JsonProperty("apiSecret")] public string ApiSecret { get; set; }
    [JsonProperty("user")] public User User { get; set; }
}