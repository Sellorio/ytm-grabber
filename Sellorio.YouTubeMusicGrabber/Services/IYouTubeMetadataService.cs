using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal interface IYouTubeMetadataService
{
    Task<YouTubeMetadata> FetchMetadataAsync(string youtubeId);
}