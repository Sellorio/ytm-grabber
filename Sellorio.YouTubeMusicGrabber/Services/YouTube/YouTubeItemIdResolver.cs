using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Exceptions;
using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Services.Common;
using Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube;

internal class YouTubeItemIdResolver(IYouTubeXhrService youTubeXhrService, IYouTubePageService youTubePageService) : IItemIdResolver
{
    public async Task<string> ResolveItemIdAsync(string urlId)
    {
        var playerApiData = await youTubeXhrService.GetPlayerAsync(urlId);
        await Task.Delay(500);

        bool isUnplayable;
        string unplayableReason;

        try
        {
            var probabilityStatus = playerApiData["playabilityStatus"];
            isUnplayable = probabilityStatus.Get<string>("status") is "UNPLAYABLE" or "ERROR";
            unplayableReason = isUnplayable ? probabilityStatus.Get<string>("reason") : null;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }

        // when this happens, the "watch" page source will contain a valid alternative video id to use.
        if (isUnplayable &&
                (unplayableReason == "This video is not available" ||
                unplayableReason.StartsWith("This video is no longer available due to a copyright claim by")))
        {
            for (var i = 0; i < 5; i++)
            {
                var pageData = await youTubePageService.GetPageInitialDataAsync("https://music.youtube.com/watch?v=" + urlId);
                var initialEndpoint = pageData[0].Get<string>("INITIAL_ENDPOINT");
                var videoId = JsonNavigator.FromString(initialEndpoint)["watchEndpoint"].Get<string>("videoId");

                if (videoId == urlId)
                {
                    throw new TrackUnavailableException(urlId);
                }
                else if (!string.IsNullOrEmpty(videoId))
                {
                    return videoId;
                }

                await Task.Delay(500);
            }

            throw new InvalidOperationException("Failed to retrieve latest video id due to an unknown issue.");
        }

        return urlId;
    }
}
