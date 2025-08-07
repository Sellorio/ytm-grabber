using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sellorio.YouTubeMusicGrabber.Commands;
using Sellorio.YouTubeMusicGrabber.Services;

var services = new ServiceCollection();
services.AddHttpClient();
services.AddScoped<IFileMetadataService, FileMetadataService>();
services.AddHttpClient<IMusicBrainzService, MusicBrainzService>(o =>
{
    o.BaseAddress = new Uri("https://musicbrainz.org/ws/2/");
    o.DefaultRequestHeaders.UserAgent.ParseAdd("ytmgrabber/1.0");
});
services.AddScoped<IYouTubeDownloadService, YouTubeDownloadService>();
services.AddScoped<IYouTubeMetadataService, YouTubeMetadataService>();
services.AddScoped<IYouTubeUriService, YouTubeUriService>();
var serviceProvider = services.BuildServiceProvider();

var parserResult =
    CommandLine.Parser.Default.ParseArguments(
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
