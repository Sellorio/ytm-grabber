using System.Text.Json.Serialization;

namespace Sellorio.YouTubeMusicGrabber.Models.YouTube;

internal class Thumbnail
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("preference")]
    public int Preference { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("resolution")]
    public string Resolution { get; set; }
}
