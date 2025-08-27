using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models;

namespace Sellorio.YouTubeMusicGrabber.Services.Common;
internal interface IFileTagsService
{
    Task UpdateFileMetadataAsync(string filename, ListMetadata albumMetadata, ItemMetadata trackMetadata);
}