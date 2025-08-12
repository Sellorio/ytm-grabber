using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal class YouTubeDownloadService(IYouTubeApiService youTubeApiService) : IYouTubeDownloadService
{
    public async Task DownloadAsMp3Async(YouTubeTrackMetadata metadata, string outputFilename, int outputBitrateKbps)
    {
        var downloadFilenameWithoutExtension = Path.GetFileNameWithoutExtension(metadata.Filename);

        try
        {
            await youTubeApiService.DownloadAsync(metadata.Id);

            if (File.Exists(downloadFilenameWithoutExtension + ".mp4"))
            {
                await ConvertToMp3Async(downloadFilenameWithoutExtension + ".mp4", outputFilename, outputBitrateKbps);
            }

            if (File.Exists(downloadFilenameWithoutExtension + ".webm"))
            {
                await ConvertToMp3Async(downloadFilenameWithoutExtension + ".webm", outputFilename, outputBitrateKbps);
            }
        }
        finally
        {
            if (File.Exists(downloadFilenameWithoutExtension + ".mp4"))
            {
                File.Delete(downloadFilenameWithoutExtension + ".mp4");
            }

            if (File.Exists(downloadFilenameWithoutExtension + ".webm"))
            {
                File.Delete(downloadFilenameWithoutExtension + ".webm");
            }
        }
    }

    private static async Task ConvertToMp3Async(string source, string destination, int outputBitrateKbps)
    {
        var startInfo =
            new ProcessStartInfo(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "ffmpeg.exe"),
                $"-y -i \"{source}\" -id3v2_version 3 -write_id3v1 1 -ab {outputBitrateKbps}k \"{destination}\"")
            {
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        var process = Process.Start(startInfo);

        // make sure the output buffer doesn't fill and block the process
        var errorOutputTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(await errorOutputTask);
        }
    }
}
