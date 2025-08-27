using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.Common;

internal interface IItemIdResolver : IItemSourceService
{
    Task<string> ResolveItemIdAsync(string urlId);
}
