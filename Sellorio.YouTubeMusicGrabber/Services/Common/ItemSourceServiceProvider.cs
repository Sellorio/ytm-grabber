using System;
using Microsoft.Extensions.DependencyInjection;
using Sellorio.YouTubeMusicGrabber.Models;
using Sellorio.YouTubeMusicGrabber.Services.Common.Registries;

namespace Sellorio.YouTubeMusicGrabber.Services.Common;

internal class ItemSourceServiceProvider(IServiceProvider serviceProvider, IItemSourceServiceRegistry itemSourceServiceRegistry) : IItemSourceServiceProvider
{
    public TService GetRequiredService<TService>(ItemSource itemSource)
        where TService : class, IItemSourceService
    {
        var serviceInterfaceType = typeof(TService);
        Type serviceImplementationType;

        if (serviceInterfaceType == typeof(IItemIdResolver))
        {
            serviceImplementationType = itemSourceServiceRegistry.GetServiceTypes(itemSource).ItemIdResolver;
        }
        else if (serviceInterfaceType == typeof(IItemMetadataService))
        {
            serviceImplementationType = itemSourceServiceRegistry.GetServiceTypes(itemSource).ItemMetadataService;
        }
        else if (serviceInterfaceType == typeof(IItemDownloadService))
        {
            serviceImplementationType = itemSourceServiceRegistry.GetServiceTypes(itemSource).ItemDownloadService;
        }
        else
        {
            throw new NotSupportedException("Unexpected item source service requested.");
        }

        return (TService)serviceProvider.GetRequiredService(serviceImplementationType);
    }
}
