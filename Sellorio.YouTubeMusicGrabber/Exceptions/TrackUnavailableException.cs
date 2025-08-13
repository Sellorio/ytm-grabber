using System;

namespace Sellorio.YouTubeMusicGrabber.Exceptions;

internal class TrackUnavailableException(string youTubeId) : ApplicationException("The track is not available on YouTube anymore.")
{
    public string YouTubeId => youTubeId;
    public string ConsoleMessage => $"Track {YouTubeId} is not available on YouTube anymore.";
}
