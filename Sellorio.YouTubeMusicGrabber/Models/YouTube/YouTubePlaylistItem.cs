using System.Text.Json.Serialization;

namespace Sellorio.YouTubeMusicGrabber.Models.YouTube;

internal class YouTubePlaylistItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }
}
