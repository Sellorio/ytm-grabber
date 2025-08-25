using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models.YouTube;

internal class YouTubePlaylistMetadata
{
    public IList<YouTubeVideoMetadata> Tracks { get; set; }
}
