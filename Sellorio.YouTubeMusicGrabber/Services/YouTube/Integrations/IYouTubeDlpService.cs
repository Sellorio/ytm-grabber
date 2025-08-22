using System.Collections.Generic;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations;

internal interface IYouTubeDlpService
{
    Task DownloadAsync(string youTubeId);
    Task<IList<YouTubePlaylistItem>> GetPlaylistEntriesAsync(string youTubeId);
    Task<YouTubeTrackMetadata> GetTrackMetadataAsync(string youTubeId);
}