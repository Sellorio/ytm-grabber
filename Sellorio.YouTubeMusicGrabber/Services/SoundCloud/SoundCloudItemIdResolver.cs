using System;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Services.Common;
using SoundCloudExplode;

namespace Sellorio.YouTubeMusicGrabber.Services.SoundCloud;

internal class SoundCloudItemIdResolver(SoundCloudClient soundCloudClient) : IItemIdResolver
{
    public async Task<string> ResolveItemIdAsync(string urlId)
    {
        var trackUrl = $"https://soundcloud.com/{urlId}";
        var track = await soundCloudClient.Tracks.GetAsync(trackUrl);
        return track.Id.ToString();
    }
}
