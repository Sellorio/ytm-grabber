using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sellorio.YouTubeMusicGrabber.Models.YouTube.Dtos;

internal class TrackMetadataDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    // Overridden from YouTube API results since this value is not set to the true, displayed
    // title of the track. For example, if there's a Japanese track with english translation,
    // sometimes the title is not the Japanese version.
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("alt_title")]
    public string AlternateTitle { get; set; }

    [JsonPropertyName("album")]
    public string Album { get; set; }

    [JsonPropertyName("artists")]
    public IList<string> Artists { get; set; }

    [JsonPropertyName("categories")]
    public IList<string> Categories { get; set; }

    [JsonPropertyName("thumbnail")]
    public string ThumbnailUri { get; set; }

    [JsonPropertyName("thumbnails")]
    public IList<ThumbnailDto> Thumbnails { get; set; }

    [JsonPropertyName("formats")]
    public IList<FormatDto> Formats { get; set; }

    [JsonPropertyName("release_date")]
    public string ReleaseDateYYYYMMDD { get; set; }

    [JsonPropertyName("release_year")]
    public int? ReleaseYear { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; }
}
