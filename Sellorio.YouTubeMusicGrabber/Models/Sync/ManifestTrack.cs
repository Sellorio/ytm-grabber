using System;

namespace Sellorio.YouTubeMusicGrabber.Models.Sync;

internal class ManifestTrack
{
    public Guid MusicBrainzId { get; set; }
    public string YouTubeId { get; set; }
    public string FileName { get; set; }
}
