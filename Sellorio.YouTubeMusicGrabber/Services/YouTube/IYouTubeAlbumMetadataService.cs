using Sellorio.YouTubeMusicGrabber.Models;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube
{
    internal interface IYouTubeAlbumMetadataService
    {
        Task<ListMetadata> GetMetadataAsync(string youTubeId);
    }
}