using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube;

internal interface IYouTubeTrackMetadataService
{
    Task<YouTubeVideoMetadata> GetMetadataAsync(string youTubeId);
    Task<string> GetLatestYouTubeIdAsync(string youTubeId);
}