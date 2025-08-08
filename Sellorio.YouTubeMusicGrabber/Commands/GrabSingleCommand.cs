using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Sellorio.YouTubeMusicGrabber.Commands.Options;
using Sellorio.YouTubeMusicGrabber.Services;

namespace Sellorio.YouTubeMusicGrabber.Commands;

[Verb("grab-single", aliases: ["gs"], HelpText = "Grabs the audio of a single youtube video.")]
internal class GrabSingleCommand : ICommand
{
    [Value(0, Required = true, HelpText = "YouTube/YouTube Music Uri.")]
    public string Uri { get; set; }

    [Option('q', "output-quality", HelpText = "Output quality (Medium - 128k, High - 256k, VeryHigh - 320k).")]
    public Quality? Quality { get; set; }

    [Option('o', "output-filename", Required = true)]
    public string OutputFilename { get; set; }

    public async Task ExecuteAsync(IServiceProvider serviceProvider)
    {
        if (Uri == null || OutputFilename == null)
        {
            throw new InvalidOperationException();
        }

        var outputFilenameAbsolute = Path.GetFullPath(OutputFilename);

        var musicBrainzService = serviceProvider.GetRequiredService<IMusicBrainzService>();
        var youTubeDownloadService = serviceProvider.GetRequiredService<IYouTubeDownloadService>();
        var youTubeMetadataService = serviceProvider.GetRequiredService<IYouTubeMetadataService>();
        var youTubeUriService = serviceProvider.GetRequiredService<IYouTubeUriService>();
        var fileMetadataService = serviceProvider.GetRequiredService<IFileMetadataService>();

        var youTubeUri = youTubeUriService.ParseVideoId(Uri);
        var youTubeMetadata = await youTubeMetadataService.FetchMetadataAsync(youTubeUri);
        var musicBrainzMetadata = await musicBrainzService.FindRecordingAsync(youTubeMetadata.Album, youTubeMetadata.Artists[0], youTubeMetadata.Title, youTubeMetadata.ReleaseDate);
        await youTubeDownloadService.DownloadAsMp3Async(youTubeMetadata, outputFilenameAbsolute, (int)(Quality ?? Options.Quality.High));

        try
        {
            await fileMetadataService.UpdateFileMetadataAsync(outputFilenameAbsolute, youTubeMetadata, musicBrainzMetadata);
        }
        catch
        {
            File.Delete(outputFilenameAbsolute);
            throw;
        }
    }
}
