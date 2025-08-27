using System.Collections.Generic;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations;

internal interface IYouTubeDlpService
{
    Task DownloadAsync(string youTubeId);
    Task<IList<PlaylistItemDto>> GetPlaylistEntriesAsync(string youTubeId);
    Task<TrackMetadataDto> GetTrackMetadataAsync(string youTubeId);
}