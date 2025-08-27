using System;
using System.IO;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Models;
using Sellorio.YouTubeMusicGrabber.Services.Common;
using SoundCloudExplode;

namespace Sellorio.YouTubeMusicGrabber.Services.SoundCloud;

internal class SoundCloudItemDownloadService(SoundCloudClient soundCloudClient) : IItemDownloadService
{
    public async Task DownloadAsMp3Async(ItemMetadata metadata, string outputFilename, int outputBitrateKbps)
    {
        ConsoleHelper.WriteLine($"Downloading track {metadata.Id}...", ConsoleColor.Cyan);

        var tempFilename = Path.Combine(Path.GetTempPath(), metadata.FilenameHint);
        var soundCloundTrack = await soundCloudClient.Tracks.GetByIdAsync(long.Parse(metadata.Id));
        await soundCloudClient.DownloadAsync(soundCloundTrack, tempFilename);

        try
        {
            await FfmpegHelper.ConvertToMp3Async(tempFilename, outputFilename, outputBitrateKbps, loudnessNormalization: true);
        }
        finally
        {
            File.Delete(tempFilename);
        }
    }
}
