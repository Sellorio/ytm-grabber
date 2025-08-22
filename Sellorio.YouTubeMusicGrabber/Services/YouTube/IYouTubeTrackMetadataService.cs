using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube;

internal interface IYouTubeTrackMetadataService
{
    Task<YouTubeTrackMetadata> GetMetadataAsync(string youTubeId);
    Task<string> GetLatestYouTubeIdAsync(string youTubeId);
}