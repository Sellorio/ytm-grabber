using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Sellorio.YouTubeMusicGrabber.Commands.Options;
using Sellorio.YouTubeMusicGrabber.Models;
using Sellorio.YouTubeMusicGrabber.Services.Common;
using Sellorio.YouTubeMusicGrabber.Services.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Commands;

[Verb("grab", HelpText = "Grabs the audio of a single youtube video/album/playlist.")]
internal class GrabSingleCommand : ICommand
{
    [Value(0, Required = true, HelpText = "YouTube/YouTube Music Uri.")]
    public string Uri { get; set; }

    [Option('q', "output-quality", HelpText = "Output quality (Medium - 196k, High - 256k, VeryHigh - 320k).")]
    public Quality? Quality { get; set; }

    [Option('o', "output-filename", Required = true)]
    public string OutputFilename { get; set; }

    public async Task ExecuteAsync(IServiceProvider serviceProvider)
    {
        if (Uri == null || OutputFilename == null)
        {
            throw new InvalidOperationException();
        }

        var itemSourceServiceProvider = serviceProvider.GetRequiredService<IItemSourceServiceProvider>();
        var youTubeUriService = serviceProvider.GetRequiredService<IYouTubeUriService>();
        var youTubeFileTagsService = serviceProvider.GetRequiredService<IFileTagsService>();
        var youTubeAlbumMetadataService = serviceProvider.GetRequiredService<IYouTubeAlbumMetadataService>();
        var itemIdResolver = itemSourceServiceProvider.GetRequiredService<IItemIdResolver>(ItemSource.YouTube);
        var itemMetadataService = itemSourceServiceProvider.GetRequiredService<IItemMetadataService>(ItemSource.YouTube);
        var youTubeDownloadService = itemSourceServiceProvider.GetRequiredService<IItemDownloadService>(ItemSource.YouTube);

        if (youTubeUriService.TryParseTrackId(Uri, out var youTubeId))
        {
            await GrabTrackAsync(itemIdResolver, itemMetadataService, youTubeAlbumMetadataService, youTubeDownloadService, youTubeFileTagsService, youTubeId);
        }
        else
        {
            throw new ArgumentException("The given URI does not link to a video.");
        }
    }

    private async Task GrabTrackAsync(
        IItemIdResolver itemIdResolver,
        IItemMetadataService itemMetadataService,
        IYouTubeAlbumMetadataService youTubeAlbumMetadataService,
        IItemDownloadService downloadService,
        IFileTagsService youTubeFileTagsService,
        string youTubeId)
    {
        var latestYouTubeId = await itemIdResolver.ResolveItemIdAsync(youTubeId);
        var trackMetadata = await itemMetadataService.GetMetadataAsync(latestYouTubeId);
        var albumMetadata = await youTubeAlbumMetadataService.GetMetadataAsync(trackMetadata.MusicMetadata.AlbumId);

        await downloadService.DownloadAsMp3Async(trackMetadata, OutputFilename, (int)(Quality ?? Options.Quality.High));

        try
        {
            await youTubeFileTagsService.UpdateFileMetadataAsync(OutputFilename, albumMetadata, trackMetadata);
        }
        catch
        {
            File.Delete(OutputFilename);
            throw;
        }
    }
}
