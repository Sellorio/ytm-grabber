using System.Text.Json.Serialization;

namespace Sellorio.YouTubeMusicGrabber.Models.YouTube;

internal class Format
{
    [JsonPropertyName("format_id")]
    public string Id { get; set; }

    [JsonPropertyName("format_note")]
    public string Note { get; set; }

    [JsonPropertyName("audio_ext")]
    public string AudioExtension { get; set; }

    [JsonPropertyName("video_ext")]
    public string VideoExtension { get; set; }

    [JsonPropertyName("resolution")]
    public string Resolution { get; set; }
}
