using Sellorio.YouTubeMusicGrabber.Models;
using Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube
{
    internal class YouTubeAlbumMetadataService(IYouTubeDlpService youTubeDlpService, IYouTubePageService youTubePageService) : IYouTubeAlbumMetadataService
    {
        private static readonly Dictionary<string, ListMetadata> _cache = new();
        private static readonly SemaphoreSlim _cacheLock = new(1);

        public async Task<ListMetadata> GetMetadataAsync(string youTubeId)
        {
            await _cacheLock.WaitAsync();

            try
            {
                if (_cache.TryGetValue(youTubeId, out ListMetadata metadata))
                {
                    return metadata;
                }
            }
            finally
            {
                _cacheLock.Release();
            }

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

            var result =
                new ListMetadata(
                    youTubeId,
                    new ListMusicMetadata(title, albumArtists, releaseYear),
                    tracks.Select(x => new ListEntry(x.Id, x.Title)).ToArray());

            await _cacheLock.WaitAsync();

            try
            {
                _cache[youTubeId] = result;
            }
            finally
            {
                _cacheLock.Release();
            }

            return result;
        }
    }
}
