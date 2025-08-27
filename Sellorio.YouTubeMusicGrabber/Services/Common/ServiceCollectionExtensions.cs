using Microsoft.Extensions.DependencyInjection;
using Sellorio.YouTubeMusicGrabber.Services.Common.Registries;

namespace Sellorio.YouTubeMusicGrabber.Services.Common;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommonServices(
        this IServiceCollection services,
        out RateLimitService rateLimitService,
        out ItemSourceServiceRegistry itemSourceServiceRegistry)
    {
        rateLimitService = new RateLimitService();
        itemSourceServiceRegistry = new ItemSourceServiceRegistry(services);

        services.AddSingleton<IRateLimitService>(rateLimitService);
        services.AddSingleton<IItemSourceServiceRegistry>(itemSourceServiceRegistry);

        services.AddScoped<IItemSourceServiceProvider, ItemSourceServiceProvider>();
        services.AddScoped<IFileTagsService, FileTagsService>();

        return services;
    }
}
