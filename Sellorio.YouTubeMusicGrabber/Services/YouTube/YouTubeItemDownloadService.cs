using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Models;
using Sellorio.YouTubeMusicGrabber.Services.Common;
using Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube;

internal class YouTubeItemDownloadService(IYouTubeDlpService youTubeDlpService) : IItemDownloadService
{
    public async Task DownloadAsMp3Async(ItemMetadata metadata, string outputFilename, int outputBitrateKbps)
    {
        for (var i = 0; i < 2; i++)
        {
            var downloadFilenameWithoutExtension = Path.GetFileNameWithoutExtension(metadata.FilenameHint);

            try
            {
                ConsoleHelper.WriteLine($"Downloading track {metadata.Id}...", ConsoleColor.Cyan);

                await youTubeDlpService.DownloadAsync(metadata.Id);

                if (File.Exists(downloadFilenameWithoutExtension + ".mp4"))
                {
                    await FfmpegHelper.ConvertToMp3Async(downloadFilenameWithoutExtension + ".mp4", outputFilename, outputBitrateKbps, false);
                }
                else if (File.Exists(downloadFilenameWithoutExtension + ".webm"))
                {
                    await FfmpegHelper.ConvertToMp3Async(downloadFilenameWithoutExtension + ".webm", outputFilename, outputBitrateKbps, false);
                }
                else if (File.Exists(downloadFilenameWithoutExtension + ".mkv"))
                {
                    await FfmpegHelper.ConvertToMp3Async(downloadFilenameWithoutExtension + ".mkv", outputFilename, outputBitrateKbps, false);
                }
                else
                {
                    ConsoleHelper.WriteLine("Download failed. Retrying...", ConsoleColor.DarkRed);
                    continue;
                }

                break;
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

                if (File.Exists(downloadFilenameWithoutExtension + ".mkv"))
                {
                    File.Delete(downloadFilenameWithoutExtension + ".mkv");
                }
            }
        }


    }
}
