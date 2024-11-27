using Newtonsoft.Json;

namespace PanoScrobblerAlbumFixer.API;

public class User
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("key", NullValueHandling = NullValueHandling.Ignore)]
    public string Key { get; set; }

    [JsonProperty("subscriber", NullValueHandling = NullValueHandling.Ignore)]
    public short Subscriber { get; set; }


    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Password { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? SessionId { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? CsrfToken { get; set; }
}