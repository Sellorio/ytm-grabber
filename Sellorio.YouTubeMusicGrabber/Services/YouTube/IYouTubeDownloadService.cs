using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube;

internal interface IYouTubeDownloadService
{
    Task DownloadAsMp3Async(YouTubeTrackMetadata metadata, string outputFilename, int outputBitrateKbps);
}