using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Exceptions;
using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;
using Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube;

internal partial class YouTubeTrackMetadataService(IYouTubeDlpService youTubeDlpService, IYouTubeXhrService youTubeXhrService, IYouTubePageService youTubePageService) : IYouTubeTrackMetadataService
{
    public async Task<string> GetLatestYouTubeIdAsync(string youTubeId)
    {
        var playerApiData = await youTubeXhrService.GetPlayerAsync(youTubeId);
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
                var pageData = await youTubePageService.GetPageInitialDataAsync("https://music.youtube.com/watch?v=" + youTubeId);
                //var response = await httpClient.GetAsync("https://music.youtube.com/watch?v=" + youTubeId);
                //response.EnsureSuccessStatusCode();
                //var responseText = await response.Content.ReadAsStringAsync();
                //var videoId = Regex.Match(responseText, @$"\\""videoId\\"":\\""({Constants.YouTubeIdRegex})\\""").Groups[1].Value;

                _ = "do nothing";

                var videoId = (string)null;

                if (videoId == youTubeId)
                {
                    throw new TrackUnavailableException(youTubeId);
                }
                else if (!string.IsNullOrEmpty(videoId))
                {
                    return videoId;
                }

                await Task.Delay(500);
            }

            throw new InvalidOperationException("Failed to retrieve latest video id due to an unknown issue.");
        }

        return youTubeId;
    }

    public async Task<YouTubeTrackMetadata> GetMetadataAsync(string youTubeId)
    {
        var metadata = await youTubeDlpService.GetTrackMetadataAsync(youTubeId);
        var nextData = await youTubeXhrService.GetNextAsync(youTubeId);

        JsonNavigator trackPlayerPanelElement;

        try
        {
            trackPlayerPanelElement =
                nextData
                    ["contents"]["singleColumnMusicWatchNextResultsRenderer"]["tabbedRenderer"]["watchNextTabbedResultsRenderer"]["tabs"][0]
                    ["tabRenderer"]["content"]["musicQueueRenderer"]["content"]["playlistPanelRenderer"]["contents"][0]
                    ["playlistPanelVideoRenderer"];
        }
        catch (KeyNotFoundException)
        {
            throw;
        }

        string trackTitle;

        try
        {
            trackTitle = trackPlayerPanelElement["title"]["runs"][0].Get<string>("text");
        }
        catch (KeyNotFoundException)
        {
            throw;
        }

        var titleSeparators = Regex.Matches(trackTitle, @" \- ");

        // For titles with translations baked in, split them up
        //   e.g. "未来キュレーション - Mirai Curation"
        // This appears to be the only option for getting the original, untranslated title
        if (titleSeparators.Count == 1)
        {
            var title1 = trackTitle.Substring(0, titleSeparators[0].Index);
            var title2 = trackTitle.Substring(titleSeparators[0].Index + 3);

            if (title1 == metadata.Title || title1 == metadata.AlternateTitle)
            {
                metadata.Title = title1;
                metadata.AlternateTitle = title2;
            }
        }

        JsonNavigator byLineSections;

        try
        {
            byLineSections = trackPlayerPanelElement["longBylineText"]["runs"];
        }
        catch (KeyNotFoundException)
        {
            throw;
        }

        if (metadata.Artists == null)
        {
            metadata.Artists = [];

            for (var i = 0; i < byLineSections.ArrayLength - 3; i += 2)
            {
                var artistName = byLineSections[i].Get<string>("text");
                metadata.Artists.Add(artistName);
            }
        }

        var albumSection = byLineSections[byLineSections.ArrayLength - 3];
        var albumName = albumSection.Get<string>("text");

        string albumBrowseId;

        try
        {
            albumBrowseId = albumSection["navigationEndpoint"]["browseEndpoint"].Get<string>("browseId");
        }
        catch (KeyNotFoundException)
        {
            throw;
        }

        metadata.Album = albumName;
        metadata.AlbumId = await GetAlbumIdAsync(albumBrowseId);

        return metadata;
    }

    private async Task<string> GetAlbumIdAsync(string browseId)
    {
        var browsePageData = await youTubePageService.GetPageInitialDataAsync($"https://music.youtube.com/browse/{browseId}");

        _ = "do nothing";

        return null;

        //var response = await httpClient.GetAsync($"https://music.youtube.com/browse/{browseId}");

        //if (!response.IsSuccessStatusCode)
        //{
        //    var responseBody = await response.Content.ReadAsStringAsync();
        //    throw new InvalidOperationException("Failed to get playlist id.\r\n" + responseBody);
        //}

        //var responseText = await response.Content.ReadAsStringAsync();

        //var albumIdMatch = Regex.Match(responseText, @$"\\x22playlistId\\x22:\\x22(OLAK5uy_{Constants.YouTubeIdRegex})\\x22");

        //if (!albumIdMatch.Success)
        //{
        //    throw new InvalidOperationException("Unable to find album id for track.");
        //}

        //return albumIdMatch.Groups[1].Value;
    }
}
