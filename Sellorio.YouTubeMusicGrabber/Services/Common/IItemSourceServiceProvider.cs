using Sellorio.YouTubeMusicGrabber.Models;

namespace Sellorio.YouTubeMusicGrabber.Services.Common;
internal interface IItemSourceServiceProvider
{
    TService GetRequiredService<TService>(ItemSource itemSource) where TService : class, IItemSourceService;
}