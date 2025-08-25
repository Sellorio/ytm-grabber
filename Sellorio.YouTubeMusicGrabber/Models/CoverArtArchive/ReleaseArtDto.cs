using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models.CoverArtArchive
{
    internal class ReleaseArtDto
    {
        public string Release { get; set; }
        public IList<ReleaseArtImageDto> Images { get; set; }
    }
}
