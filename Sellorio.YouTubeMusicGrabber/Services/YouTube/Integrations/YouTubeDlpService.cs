using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Exceptions;
using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;
using Sellorio.YouTubeMusicGrabber.Models.YouTube.Dtos;
using Sellorio.YouTubeMusicGrabber.Services.Common;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations;

internal class YouTubeDlpService(IRateLimitService rateLimitService) : IYouTubeDlpService
{
    public async Task<TrackMetadataDto> GetTrackMetadataAsync(string youTubeId)
    {
        ConsoleHelper.WriteLine($"Retrieving track metadata for {youTubeId}...", ConsoleColor.DarkGray);

        TrackMetadataDto result = null;

        await rateLimitService.WithRateLimit(RateLimits.DlpTrackInfo, async () =>
        {
            var json = await InvokeYtDlpAsync(youTubeId, $"--cookies cookies.txt --print-json --skip-download \"https://music.youtube.com/watch?v={youTubeId}\"");
            result = JsonSerializer.Deserialize<TrackMetadataDto>(json);
        });

        return result;
    }

    public async Task<IList<YouTubePlaylistItem>> GetPlaylistEntriesAsync(string youTubeId)
    {
        ConsoleHelper.WriteLine($"Retrieving tracks list for playlist {youTubeId}...", ConsoleColor.DarkGray);

        IList<YouTubePlaylistItem> result = null;

        await rateLimitService.WithRateLimit(RateLimits.DlpPlaylistInfo, async () =>
        {
            var output = await InvokeYtDlpAsync(youTubeId, $"--cookies cookies.txt --flat-playlist --print-json --skip-download \"https://music.youtube.com/playlist?list={youTubeId}\"");
            var jsons = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            result = jsons.Select(x => JsonSerializer.Deserialize<YouTubePlaylistItem>(x)).ToList();
        });

        return result;
    }

    public async Task DownloadAsync(string youTubeId)
    {
        ConsoleHelper.WriteLine($"Downloading track {youTubeId}...", ConsoleColor.Cyan);

        await rateLimitService.WithRateLimit([RateLimits.DlpDownload, RateLimits.DlpTrackInfo], async () =>
        {
            await InvokeYtDlpAsync(youTubeId, $"--cookies cookies.txt --ffmpeg-location . \"https://music.youtube.com/watch?v={youTubeId}\"");
        });
    }

    private static async Task<string> InvokeYtDlpAsync(string youTubeId, string arguments)
    {
        var startInfo =
            new ProcessStartInfo(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "yt-dlp.exe"),
                arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        var process = new Process { StartInfo = startInfo };
        process.Start();

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();

        await process!.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var errorOutput = await process.StandardError.ReadToEndAsync();

            if (Regex.IsMatch(errorOutput, @$"^ERROR: \[youtube\] {Constants.YouTubeIdRegex}: Video unavailable. This video is not available"))
            {
                throw new TrackUnavailableException(youTubeId);
            }

            throw new InvalidOperationException(errorOutput);
        }

        return await standardOutputTask;
    }
}
