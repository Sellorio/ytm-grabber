using System;

namespace Sellorio.YouTubeMusicGrabber.Services.Common.Registries;

internal class ItemSourceServiceTypes(Type itemIdResolver, Type itemMetadataService, Type itemDownloadService)
{
    public Type ItemIdResolver => itemIdResolver;
    public Type ItemMetadataService => itemMetadataService;
    public Type ItemDownloadService => itemDownloadService;
}
