using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sellorio.YouTubeMusicGrabber.Models.YouTube;

internal class YouTubeTrackMetadata
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

    // Retrieved after the YouTube API call by reading the static JSON on the YouTube Music
    // web page for the song.
    [JsonIgnore]
    public string AlbumId { get; set; }

    [JsonPropertyName("artists")]
    public string[] Artists { get; set; }

    [JsonPropertyName("categories")]
    public string[] Categories { get; set; }

    [JsonPropertyName("thumbnail")]
    public string ThumbnailUri { get; set; }

    [JsonPropertyName("thumbnails")]
    public IList<Thumbnail> Thumbnails { get; set; }

    [JsonPropertyName("formats")]
    public IList<Format> Formats { get; set; }

    [JsonPropertyName("release_date")]
    public string ReleaseDateYYYYMMDD { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; }

    [JsonIgnore]
    public DateOnly ReleaseDate =>
        new(int.Parse(ReleaseDateYYYYMMDD.AsSpan(0, 4)),
            int.Parse(ReleaseDateYYYYMMDD.AsSpan(4, 2)),
            int.Parse(ReleaseDateYYYYMMDD.AsSpan(6, 2)));
}
