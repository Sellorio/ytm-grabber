using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models.YouTube
{
    internal class YouTubeAlbumMetadata
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string[] Artists { get; set; }
        public int? ReleaseYear { get; set; }
        public IList<YouTubePlaylistItem> Tracks { get; set; }
    }
}
