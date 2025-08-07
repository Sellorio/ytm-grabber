using System;
using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;

internal class Artist
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string SortName { get; set; }
    // this is used for fictional characters credited as song artists (may be used for other things too though)
    public string Disambiguation { get; set; }
    public IList<ArtistAlias> Aliases { get; set; }
}
