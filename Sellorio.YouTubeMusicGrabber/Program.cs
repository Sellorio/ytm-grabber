using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sellorio.YouTubeMusicGrabber.Commands;
using Sellorio.YouTubeMusicGrabber.Services;
using Sellorio.YouTubeMusicGrabber.Services.Common;
using Sellorio.YouTubeMusicGrabber.Services.YouTube;

Console.OutputEncoding = System.Text.Encoding.UTF8;


var services = new ServiceCollection();
services.AddHttpClient();

services.AddHttpClient<IMusicBrainzService, MusicBrainzService>(o =>
{
    o.BaseAddress = new Uri("https://musicbrainz.org/ws/2/");
    o.DefaultRequestHeaders.UserAgent.ParseAdd("ytmgrabber/1.0");
});

var rateLimitService = new RateLimitService();

services.AddYouTubeServices(rateLimitService);
services.AddSingleton<IRateLimitService>(rateLimitService);
services.AddScoped<IYouTubeFileTagsService, YouTubeFileTagsService>();
services.AddScoped<ISyncService, SyncService>();

var serviceProvider = services.BuildServiceProvider();

var parser = new CommandLine.Parser(o => o.AllowMultiInstance = true);

var parserResult =
    parser.ParseArguments(
        args,
        typeof(ICommand).Assembly
            .GetTypes()
            .Where(typeof(ICommand).IsAssignableFrom)
            .ToArray());

foreach (var error in parserResult.Errors)
{
    Console.Error.WriteLine(error.ToString());
}

if (parserResult.Value is ICommand command)
{
    using var scope = serviceProvider.CreateScope();
    await command.ExecuteAsync(scope.ServiceProvider);
}




