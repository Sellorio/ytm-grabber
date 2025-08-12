using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;

internal class Release
{
    public Guid Id { get; set; }
    public string Status { get; set; }
    public int TrackCount { get; set; }
    public string Title { get; set; }
    public string Country { get; set; } // XW = Worldwide
    public ReleaseGroup ReleaseGroup { get; set; }
    public IList<ArtistCreditItem> ArtistCredit { get; set; }
    public IList<Medium> Media { get; set; }

    [JsonIgnore]
    public DateOnly? Date => DateOnly.TryParse(DateString, out var result) ? result : null;
    [JsonIgnore]
    public int? ReleaseYear => Date?.Year ?? (int.TryParse(DateString, out var year) ? year : null);

    [JsonPropertyName("date")]
    public string DateString { get; set; }
}
