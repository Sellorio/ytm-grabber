using System.Collections.Generic;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal interface IYouTubeApiService
{
    Task DownloadAsync(string youTubeId);
    Task<IList<YouTubePlaylistItem>> GetPlaylistEntriesAsync(string youTubeId);
    Task<YouTubeTrackMetadata> GetTrackMetadataAsync(string youTubeId);
}