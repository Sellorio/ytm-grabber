using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;

internal class RecordSearchQuery
{
    public IList<string> PossibleTitles { get; set; }
    public string Album { get; set; }
    public IList<string> Artists { get; set; }
}
