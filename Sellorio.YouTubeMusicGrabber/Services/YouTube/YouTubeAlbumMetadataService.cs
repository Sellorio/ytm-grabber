using Sellorio.YouTubeMusicGrabber.Models.YouTube;
using Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube
{
    internal class YouTubeAlbumMetadataService(IYouTubeDlpService youTubeDlpService, IYouTubePageService youTubePageService) : IYouTubeAlbumMetadataService
    {
        public async Task<YouTubeAlbumMetadata> GetMetadataAsync(string youTubeId)
        {
            var tracks = await youTubeDlpService.GetPlaylistEntriesAsync(youTubeId);
            var pageData = await youTubePageService.GetPageInitialDataAsync($"https://music.youtube.com/playlist?list={youTubeId}");

            return new YouTubeAlbumMetadata
            {
                Id = youTubeId,
                Tracks = tracks
            };
        }
    }
}
