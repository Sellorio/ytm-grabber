using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Sellorio.YouTubeMusicGrabber.Models;

namespace Sellorio.YouTubeMusicGrabber.Services.Common.Registries;

internal class ItemSourceServiceRegistry(IServiceCollection services) : IItemSourceServiceRegistry
{
    private readonly Dictionary<ItemSource, ItemSourceServiceTypes> _itemSourceServices = [];

    public void RegisterServices<TItemIdResolver, TItemMetadataService, TItemDownloadService>(ItemSource itemSource)
        where TItemIdResolver : class, IItemIdResolver
        where TItemMetadataService : class, IItemMetadataService
        where TItemDownloadService : class, IItemDownloadService
    {
        if (_itemSourceServices.ContainsKey(itemSource))
        {
            throw new InvalidOperationException("Services have already been registered for this item source.");
        }

        _itemSourceServices.Add(itemSource, new(typeof(TItemIdResolver), typeof(TItemMetadataService), typeof(TItemDownloadService)));

        services.AddScoped<TItemIdResolver>();
        services.AddScoped<TItemMetadataService>();
        services.AddScoped<TItemDownloadService>();
    }

    public ItemSourceServiceTypes GetServiceTypes(ItemSource itemSource)
    {
        if (!_itemSourceServices.TryGetValue(itemSource, out var result))
        {
            result = null;
        }

        return result;
    }
}
