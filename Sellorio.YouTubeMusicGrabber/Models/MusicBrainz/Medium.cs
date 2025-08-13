using System;
using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;

internal class Medium
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public string Format { get; set; }
    public int TrackCount { get; set; }
    public int TrackOffset { get; set; }
    public IList<Track> Track { get; set; }
    public IList<Track> Tracks { get; set; }
}
