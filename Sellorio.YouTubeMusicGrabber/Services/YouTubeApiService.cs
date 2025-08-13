using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Exceptions;
using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal class YouTubeApiService : IYouTubeApiService
{
    private const int ThrottleTime = 10_000;
    private const bool ThrottleDownloads = true;
    private const bool ThrottleTrackInfo = true;
    private const bool ThrottlePlaylistInfo = false;

    private readonly SemaphoreSlim _semaphore = new(1);
    private DateTime? _lastThrottledCall;

    public async Task<YouTubeTrackMetadata> GetTrackMetadataAsync(string youTubeId)
    {
        Console.WriteLine($"Retrieving track metadata for {youTubeId}...");

        YouTubeTrackMetadata result = null;

        await WithThrottlingIfEnabled(ThrottleTrackInfo, async () =>
        {
            var json = await InvokeYtDlpAsync(youTubeId, $"--cookies cookies.txt --print-json --skip-download \"https://music.youtube.com/watch?v={youTubeId}\"");
            result = JsonSerializer.Deserialize<YouTubeTrackMetadata>(json);
        });

        return result;
    }

    public async Task<IList<YouTubePlaylistItem>> GetPlaylistEntriesAsync(string youTubeId)
    {
        Console.WriteLine($"Retrieving tracks list for playlist {youTubeId}...");

        IList<YouTubePlaylistItem> result = null;

        await WithThrottlingIfEnabled(ThrottlePlaylistInfo, async () =>
        {
            var output = await InvokeYtDlpAsync(youTubeId, $"--cookies cookies.txt --flat-playlist --print-json --skip-download \"https://music.youtube.com/playlist?list={youTubeId}\"");
            var jsons = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            result = jsons.Select(x => JsonSerializer.Deserialize<YouTubePlaylistItem>(x)).ToList();
        });

        return result;
    }

    public async Task DownloadAsync(string youTubeId)
    {
        Console.WriteLine($"Downloading track {youTubeId}...");

        await WithThrottlingIfEnabled(ThrottleDownloads, async () =>
        {
            await InvokeYtDlpAsync(youTubeId, $"--cookies cookies.txt --ffmpeg-location . \"https://music.youtube.com/watch?v={youTubeId}\"");
        });
    }

    private async Task WithThrottlingIfEnabled(bool shouldThrottle, Func<Task> func)
    {
        if (shouldThrottle)
        {
            await _semaphore.WaitAsync();

            try
            {
                var waitTime =
                    _lastThrottledCall != null
                        ? _lastThrottledCall.Value.AddMilliseconds(ThrottleTime) - DateTime.UtcNow
                        : TimeSpan.Zero;

                if (waitTime > TimeSpan.Zero)
                {
                    var position = Console.GetCursorPosition();
                    Console.WriteLine("Waiting before calling YouTube APIs again...");
                    await Task.Delay(waitTime);
                    ConsoleHelper.ResetBackToPositionAndClearConsole(position);
                }

                await func.Invoke();

                _lastThrottledCall = DateTime.UtcNow;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        else
        {
            await func.Invoke();
        }
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
