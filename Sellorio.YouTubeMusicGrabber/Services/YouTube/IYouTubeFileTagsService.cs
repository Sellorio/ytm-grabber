using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube;
internal interface IYouTubeFileTagsService
{
    Task UpdateFileMetadataAsync(string filename, YouTubeAlbumMetadata albumMetadata, YouTubeTrackMetadata trackMetadata);
}