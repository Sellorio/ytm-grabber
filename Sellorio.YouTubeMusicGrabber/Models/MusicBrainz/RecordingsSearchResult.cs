using System;
using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;

internal class RecordingsSearchResult
{
    public DateTimeOffset Created { get; set; }
    public int Count { get; set; }
    public int Offset { get; set; }
    public IList<Recording> Recordings { get; set; }
}
