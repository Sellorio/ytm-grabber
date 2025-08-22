using System;
using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models.Sync;

internal class OldManifestAlbum
{
    public Guid MusicBrainzId { get; set; }
    public string YouTubeId { get; set; }
    public string FolderName { get; set; }
    public bool IsFullyDownloaded { get; set; }
    public IList<OldManifestTrack> Tracks { get; set; }
}
