using System;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Models;
using Sellorio.YouTubeMusicGrabber.Services.Common;

namespace Sellorio.YouTubeMusicGrabber.Services.SoundCloud;
internal class SoundCloudItemMetadataService(IFallbackMetadataService fallbackMetadataService) : IItemMetadataService
{
    public async Task<ItemMetadata> GetMetadataAsync(string itemId)
    {
        ConsoleHelper.WriteLine($"Retrieving track metadata for {itemId}...", ConsoleColor.DarkGray);

        var filenameHint = itemId + ".mp3";
        var musicMetadata = await fallbackMetadataService.PromptUserForMusicMetadataAsync();

        return new ItemMetadata(itemId, ItemSource.SoundCloud, musicMetadata, filenameHint);
    }
}
