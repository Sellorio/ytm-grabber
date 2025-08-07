using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services;
internal interface IFileMetadataService
{
    Task UpdateFileMetadataAsync(string filename, YouTubeMetadata youTubeMetadata, RecordingMatch musicBrainzMetadata);
}