using Microsoft.Extensions.DependencyInjection;
using Sellorio.YouTubeMusicGrabber.Models;
using Sellorio.YouTubeMusicGrabber.Services.Common.Registries;
using SoundCloudExplode;

namespace Sellorio.YouTubeMusicGrabber.Services.SoundCloud;
internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSoundCloudServices(this IServiceCollection services, ItemSourceServiceRegistry itemSourceServiceRegistry)
    {
        var soundCloudClient = new SoundCloudClient();
        soundCloudClient.InitializeAsync().GetAwaiter().GetResult();

        services.AddSingleton(soundCloudClient);
        itemSourceServiceRegistry.RegisterServices<SoundCloudItemIdResolver, SoundCloudItemMetadataService, SoundCloudItemDownloadService>(ItemSource.SoundCloud);
        services.AddScoped<ISoundCloudUriService, SoundCloudUriService>();
        return services;
    }
}
