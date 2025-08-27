using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models;

namespace Sellorio.YouTubeMusicGrabber.Services;
internal interface IFallbackMetadataService
{
    Task<ItemMusicMetadata> PromptUserForMusicMetadataAsync();
}