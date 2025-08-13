using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Sellorio.YouTubeMusicGrabber.Commands;
using Sellorio.YouTubeMusicGrabber.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var withYouTubeCookiesHandler = CreateHandlerFromCookiesFile("cookies.txt");

var services = new ServiceCollection();
services.AddHttpClient();
services.AddScoped<IFileMetadataService, FileMetadataService>();
services.AddHttpClient<IMusicBrainzService, MusicBrainzService>(o =>
{
    o.BaseAddress = new Uri("https://musicbrainz.org/ws/2/");
    o.DefaultRequestHeaders.UserAgent.ParseAdd("ytmgrabber/1.0");
});
services.AddScoped<ISyncService, SyncService>();
services.AddHttpClient<IYouTubeMetadataService, YouTubeMetadataService>(o =>
{
    o.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:141.0) Gecko/20100101 Firefox/141.0");
}).ConfigurePrimaryHttpMessageHandler(() => withYouTubeCookiesHandler);
services.AddScoped<IYouTubeDownloadService, YouTubeDownloadService>();
services.AddScoped<IYouTubeApiService, YouTubeApiService>();
services.AddScoped<IYouTubeUriService, YouTubeUriService>();
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




static HttpClientHandler CreateHandlerFromCookiesFile(string cookieFile)
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