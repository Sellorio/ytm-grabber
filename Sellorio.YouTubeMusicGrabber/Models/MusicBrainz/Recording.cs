using System;
using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;

internal class Recording
{
    public Guid Id { get; set; }
    public int Score { get; set; }
    public string Title { get; set; }
    public IList<Release> Releases { get; set; }
    public IList<ArtistCreditItem> ArtistCredit { get; set; }
}
