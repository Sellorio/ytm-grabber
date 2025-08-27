using Sellorio.YouTubeMusicGrabber.Models;

namespace Sellorio.YouTubeMusicGrabber.Services.Common.Registries;
internal interface IItemSourceServiceRegistry
{
    ItemSourceServiceTypes GetServiceTypes(ItemSource itemSource);
}