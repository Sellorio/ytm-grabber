using System;

namespace Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;

internal class Track
{
    public Guid Id { get; set; }
    public string Number { get; set; }
    public string Title { get; set; }
    public int? Length { get; set; }
    public TrackRecording Recording { get; set; }
}
