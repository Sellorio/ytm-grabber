using System;
using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models.YouTube
{
    internal class YouTubeMusicMetadata
    {
        public string Title { get; set; }

        public string AlternateTitle { get; set; }

        public IList<string> Artists { get; set; }

        public string Album { get; set; }

        public string AlbumId { get; set; }

        public int? ReleaseYear { get; set; }

        public DateOnly? ReleaseDate { get; set; }

        public int TrackNumber { get; set; }

        public int TrackCount { get; set; }

        public string AlbumArtUrl { get; set; }
    }
}
