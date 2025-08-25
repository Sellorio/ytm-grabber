using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models.CoverArtArchive
{
    internal class ReleaseArtImageDto
    {
        public bool Approved { get; set; }
        public bool Back { get; set; }
        public string Comment { get; set; }
        public long Edit { get; set; }
        public bool Front { get; set; }
        public long Id { get; set; }
        public string Image { get; set; }
        public Dictionary<string, string> Thumbnails { get; set; }
        public IList<string> Types { get; set; }
    }
}
