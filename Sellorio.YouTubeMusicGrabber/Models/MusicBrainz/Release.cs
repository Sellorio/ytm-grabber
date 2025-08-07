using System;
using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;

internal class Release
{
    public Guid Id { get; set; }
    public string Status { get; set; }
    public int TrackCount { get; set; }
    public string Title { get; set; }
    public DateOnly Date { get; set; }
    public string Country { get; set; } // XW = Worldwide
    public ReleaseGroup ReleaseGroup { get; set; }
    public IList<ArtistCreditItem> ArtistCredit { get; set; }
    public IList<Medium> Media { get; set; }
}
