using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal class YouTubeDownloadService : IYouTubeDownloadService
{
    public async Task DownloadAsMp3Async(YouTubeMetadata metadata, string outputFilename, int outputBitrateKbps)
    {
        var downloadFilenameWithoutExtension = Path.GetFileNameWithoutExtension(metadata.Filename);

        var startInfo =
            new ProcessStartInfo(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "yt-dlp.exe"),
                $"--cookies cookies.txt \"https://music.youtube.com/watch?v={metadata.Id}\"")
            {
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        var process = new Process { StartInfo = startInfo };
        process.Start();

        try
        {
            await process!.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(await process.StandardError.ReadToEndAsync());
            }

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

    private async Task ConvertToMp3Async(string source, string destination, int outputBitrateKbps)
    {
        var startInfo =
            new ProcessStartInfo(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "ffmpeg.exe"),
                $"-y -i \"{source}\" -id3v2_version 3 -write_id3v1 1 -ab {outputBitrateKbps}k \"{destination}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        var process = Process.Start(startInfo);

        // make sure the output buffer doesn't fill and block the process
        _ = process.StandardOutput.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(await process.StandardError.ReadToEndAsync());
        }
    }
}
