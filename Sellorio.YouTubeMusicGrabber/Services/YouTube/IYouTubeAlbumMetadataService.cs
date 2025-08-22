using Sellorio.YouTubeMusicGrabber.Models.YouTube;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube
{
    internal interface IYouTubeAlbumMetadataService
    {
        Task<YouTubeAlbumMetadata> GetMetadataAsync(string youTubeId);
    }
}