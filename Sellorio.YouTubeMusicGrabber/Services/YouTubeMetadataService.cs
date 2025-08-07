using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal class YouTubeMetadataService : IYouTubeMetadataService
{
    public async Task<YouTubeMetadata> FetchMetadataAsync(string youTubeId)
    {
        var startInfo =
            new ProcessStartInfo(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "yt-dlp.exe"),
                $"--print-json --skip-download \"https://music.youtube.com/watch?v={youTubeId}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        var process = new Process { StartInfo = startInfo };
        process.Start();

        var jsonTask = process.StandardOutput.ReadToEndAsync();

        await process!.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(await process.StandardError.ReadToEndAsync());
        }

        var json = await jsonTask;
        var result = JsonSerializer.Deserialize<YouTubeMetadata>(json);

        if (File.Exists(result.Filename))
        {
            File.Delete(result.Filename);
        }

        return result;
    }
}
