using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models;

namespace Sellorio.YouTubeMusicGrabber.Services.Common;

internal interface IItemMetadataService : IItemSourceService
{
    Task<ItemMetadata> GetMetadataAsync(string itemId);
}
