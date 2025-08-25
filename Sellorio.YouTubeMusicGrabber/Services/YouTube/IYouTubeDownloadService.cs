using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube;

internal interface IYouTubeDownloadService
{
    Task DownloadAsMp3Async(YouTubeVideoMetadata metadata, string outputFilename, int outputBitrateKbps);
}