using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models;

namespace Sellorio.YouTubeMusicGrabber.Services.Common;

internal interface IItemDownloadService : IItemSourceService
{
    Task DownloadAsMp3Async(ItemMetadata metadata, string outputFilename, int outputBitrateKbps);
}