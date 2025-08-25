using Sellorio.YouTubeMusicGrabber.Models.YouTube.Dtos;
using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models.YouTube;

internal class YouTubeVideoMetadata
{
    public string Id { get; set; }

    public YouTubeMusicMetadata MusicMetadata { get; set; }

    public IList<ThumbnailDto> Thumbnails { get; set; }

    public string Filename { get; set; }
}
