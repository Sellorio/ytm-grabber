using System;

namespace Sellorio.YouTubeMusicGrabber.Models.Sync;

internal class OldManifestTrack
{
    public Guid MusicBrainzId { get; set; }
    public string YouTubeId { get; set; }
    public string FileName { get; set; }
}
