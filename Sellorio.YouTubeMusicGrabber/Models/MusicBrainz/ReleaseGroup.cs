using System;

namespace Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;

internal class ReleaseGroup
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string PrimaryType { get; set; }
}
