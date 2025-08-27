using Microsoft.Extensions.DependencyInjection;
using Sellorio.YouTubeMusicGrabber.Services.Common;
using System.Net.Http;
using System.Net;
using System;
using System.IO;
using Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations;
using Sellorio.YouTubeMusicGrabber.Models;
using Sellorio.YouTubeMusicGrabber.Services.Common.Registries;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddYouTubeServices(this IServiceCollection services, RateLimitService rateLimitService, ItemSourceServiceRegistry itemSourceServiceRegistry)
    {
        rateLimitService.ConfigureRateLimit(RateLimits.DlpDownload, TimeSpan.FromSeconds(10));
        rateLimitService.ConfigureRateLimit(RateLimits.DlpTrackInfo, TimeSpan.FromSeconds(10));
        rateLimitService.ConfigureRateLimit(RateLimits.DlpPlaylistInfo, TimeSpan.FromMilliseconds(500));
        rateLimitService.ConfigureRateLimit(RateLimits.XHR, TimeSpan.FromMilliseconds(500));
        rateLimitService.ConfigureRateLimit(RateLimits.Page, TimeSpan.FromMilliseconds(500));

        itemSourceServiceRegistry.RegisterServices<YouTubeItemIdResolver, YouTubeItemMetadataService, YouTubeItemDownloadService>(ItemSource.YouTube);

        var withYouTubeCookiesHandler = CreateHandlerFromCookiesFile("cookies.txt");

        services.AddHttpClient<IYouTubeXhrService, YouTubeXhrService>(o =>
        {
            o.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:141.0) Gecko/20100101 Firefox/141.0");
        }).ConfigurePrimaryHttpMessageHandler(() => withYouTubeCookiesHandler);

        services.AddHttpClient<IYouTubePageService, YouTubePageService>(o =>
        {
            o.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:141.0) Gecko/20100101 Firefox/141.0");
        }).ConfigurePrimaryHttpMessageHandler(() => withYouTubeCookiesHandler);

        services.AddScoped<IYouTubeDlpService, YouTubeDlpService>();
        services.AddScoped<IYouTubeUriService, YouTubeUriService>();
        services.AddScoped<IYouTubeAlbumMetadataService, YouTubeAlbumMetadataService>();

        return services;
    }

    private static HttpClientHandler CreateHandlerFromCookiesFile(string cookieFile)
    {
        var cookieContainer = new CookieContainer();

        foreach (var line in File.ReadLines(cookieFile))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue; // skip comments and empty lines

            // Format: domain \t flag \t path \t secure \t expiry \t name \t value
            var parts = line.Split('\t');
            if (parts.Length != 7)
                continue; // skip invalid lines

            string domain = parts[0];
            string path = parts[2];
            bool secure = parts[3].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            string name = parts[5];
            string value = parts[6];

            cookieContainer.Add(new Cookie(name, value, path, domain.TrimStart('.')));
        }

        return new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AutomaticDecompression = DecompressionMethods.All
        };
    }
}
