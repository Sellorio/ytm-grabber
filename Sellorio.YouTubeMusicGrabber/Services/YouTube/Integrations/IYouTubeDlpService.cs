using System.Collections.Generic;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;
using Sellorio.YouTubeMusicGrabber.Models.YouTube.Dtos;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations;

internal interface IYouTubeDlpService
{
    Task DownloadAsync(string youTubeId);
    Task<IList<YouTubePlaylistItem>> GetPlaylistEntriesAsync(string youTubeId);
    Task<TrackMetadataDto> GetTrackMetadataAsync(string youTubeId);
}