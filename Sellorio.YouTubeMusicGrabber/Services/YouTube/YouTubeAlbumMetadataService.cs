using Sellorio.YouTubeMusicGrabber.Models.YouTube;
using Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations;
using System.Linq;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube
{
    internal class YouTubeAlbumMetadataService(IYouTubeDlpService youTubeDlpService, IYouTubePageService youTubePageService) : IYouTubeAlbumMetadataService
    {
        public async Task<YouTubeAlbumMetadata> GetMetadataAsync(string youTubeId)
        {
            var tracks = await youTubeDlpService.GetPlaylistEntriesAsync(youTubeId);
            var pageData = await youTubePageService.GetPageInitialDataAsync($"https://music.youtube.com/playlist?list={youTubeId}");

            var sectionJson = pageData[1]["contents"]["twoColumnBrowseResultsRenderer"]["tabs"][0]["tabRenderer"]["content"]["sectionListRenderer"]["contents"][0];

            var albumHeaderJson =
                sectionJson["musicResponsiveHeaderRenderer"] ??
                sectionJson["musicEditablePlaylistDetailHeaderRenderer"]["header"]["musicResponsiveHeaderRenderer"];

            var title = albumHeaderJson["title"]["runs"][0].Get<string>("text");
            var releaseYearString = albumHeaderJson["subtitle"]["runs"].NthFromLast(0).Get<string>("text");
            var releaseYear = !string.IsNullOrEmpty(releaseYearString) && int.TryParse(releaseYearString, out var p) ? p : (int?)null;
            var albumArtists = albumHeaderJson["straplineTextOne"]?["runs"].Select(x => x.Get<string>("text")).ToArray();

            return new YouTubeAlbumMetadata
            {
                Id = youTubeId,
                Tracks = tracks,
                Title = title,
                ReleaseYear = releaseYear,
                Artists = albumArtists
            };
        }
    }
}
