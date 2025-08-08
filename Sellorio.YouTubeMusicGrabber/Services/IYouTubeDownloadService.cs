using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal interface IYouTubeDownloadService
{
    Task DownloadAsMp3Async(YouTubeMetadata metadata, string outputFilename, int outputBitrateKbps);
}