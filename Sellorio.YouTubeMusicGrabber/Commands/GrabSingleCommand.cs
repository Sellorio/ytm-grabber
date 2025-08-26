using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Sellorio.YouTubeMusicGrabber.Commands.Options;
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

        var youTubeDownloadService = serviceProvider.GetRequiredService<IYouTubeDownloadService>();
        var youTubeUriService = serviceProvider.GetRequiredService<IYouTubeUriService>();
        var youTubeFileTagsService = serviceProvider.GetRequiredService<IYouTubeFileTagsService>();
        var youTubeAlbumMetadataService = serviceProvider.GetRequiredService<IYouTubeAlbumMetadataService>();
        var youTubeTrackMetadataService = serviceProvider.GetRequiredService<IYouTubeTrackMetadataService>();

        if (youTubeUriService.TryParseTrackId(Uri, out var youTubeId))
        {
            await GrabTrackAsync(youTubeTrackMetadataService, youTubeAlbumMetadataService, youTubeDownloadService, youTubeFileTagsService, youTubeId);
        }
        else if (youTubeUriService.TryParseAlbumId(Uri, out var albumId))
        {
            throw new NotImplementedException();
        }
        else
        {
            throw new ArgumentException("The given URI does not link to a video/album/playlist.");
        }

        
    }

    private async Task GrabTrackAsync(
        IYouTubeTrackMetadataService youTubeTrackMetadataService,
        IYouTubeAlbumMetadataService youTubeAlbumMetadataService,
        IYouTubeDownloadService youTubeDownloadService,
        IYouTubeFileTagsService youTubeFileTagsService,
        string youTubeId)
    {
        var latestYouTubeId = await youTubeTrackMetadataService.GetLatestYouTubeIdAsync(youTubeId);
        var trackMetadata = await youTubeTrackMetadataService.GetMetadataAsync(latestYouTubeId);
        var albumMetadata = await youTubeAlbumMetadataService.GetMetadataAsync(trackMetadata.MusicMetadata.AlbumId);

        await youTubeDownloadService.DownloadAsMp3Async(trackMetadata, OutputFilename, (int)(Quality ?? Options.Quality.High));

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
