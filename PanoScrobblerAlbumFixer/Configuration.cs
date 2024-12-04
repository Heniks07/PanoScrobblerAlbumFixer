using Newtonsoft.Json;
using PanoScrobblerAlbumFixer.API;

namespace PanoScrobblerAlbumFixer;

public class Configuration
{
    [JsonProperty("apiKey")] public required string ApiKey { get; set; }
    [JsonProperty("apiSecret")] public required string ApiSecret { get; set; }

    [JsonProperty("domain")] public required string Domain { get; set; }
    [JsonProperty("scrobblerDomain")] public required string ScrobblerDomain { get; set; }
    [JsonProperty("user")] public required User User { get; set; }
}