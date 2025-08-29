using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services
{
    internal interface IPrerequisiteService
    {
        Task EnsureFfmpegAsync(bool force = false);
        Task EnsureYouTubeDlpAsync(bool force = false);
    }
}