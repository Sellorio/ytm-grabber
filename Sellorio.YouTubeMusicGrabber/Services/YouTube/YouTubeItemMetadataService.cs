using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Models;
using Sellorio.YouTubeMusicGrabber.Services.Common;
using Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube;

internal partial class YouTubeItemMetadataService(
    IYouTubeDlpService youTubeDlpService,
    IYouTubeXhrService youTubeXhrService,
    IYouTubePageService youTubePageService,
    IYouTubeAlbumMetadataService youTubeAlbumMetadataService,
    IFallbackMetadataService fallbackMetadataService) : IItemMetadataService
{
    public async Task<ItemMetadata> GetMetadataAsync(string itemId)
    {
        ConsoleHelper.WriteLine($"Retrieving track metadata for {itemId}...", ConsoleColor.DarkGray);

        var metadataDto = await youTubeDlpService.GetTrackMetadataAsync(itemId);
        var nextData = await youTubeXhrService.GetNextAsync(itemId);

        var bestThumbnailPreferenceScore =
            metadataDto.Thumbnails != null && metadataDto.Thumbnails.Any()
                ? metadataDto.Thumbnails.Min(x => x.Preference)
                : 0;

        var preferredThumbnail =
            metadataDto.Thumbnails
                .Where(x => x.Preference == bestThumbnailPreferenceScore && x.Width == x.Height && x.Width < 500)
                .OrderByDescending(x => x.Width)
                .FirstOrDefault();

        if (!metadataDto.HasMusicMetadata())
        {
            ConsoleHelper.WriteLine($"{itemId} is a non-music video file.", ConsoleColor.White);
            var musicMetadata = await fallbackMetadataService.PromptUserForMusicMetadataAsync();
            return new ItemMetadata(metadataDto.Id, ItemSource.YouTube, musicMetadata, metadataDto.Filename);
        }

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

            if (title1 == metadataDto.Title || title1 == metadataDto.AlternateTitle ||
                title2 == metadataDto.Title || title2 == metadataDto.AlternateTitle)
            {
                metadataDto.Title = title1;
                metadataDto.AlternateTitle = title2;
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

        if (metadataDto.Artists == null)
        {
            metadataDto.Artists = [];

            for (var i = 0; i < byLineSections.ArrayLength - 3; i += 2)
            {
                var artistName = byLineSections[i].Get<string>("text");
                metadataDto.Artists.Add(artistName);
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

        metadataDto.Album = albumName;
        var albumId = await GetAlbumIdAsync(albumBrowseId);

        var albumMetadata = await youTubeAlbumMetadataService.GetMetadataAsync(albumId);

        var trackCount = albumMetadata.Items.Count;
        var trackNumber = albumMetadata.Items.Index().First(x => x.Item.Id == itemId).Index + 1;

        return
            new ItemMetadata(
                metadataDto.Id,
                ItemSource.YouTube,
                new ItemMusicMetadata(
                    metadataDto.Title,
                    metadataDto.AlternateTitle,
                    metadataDto.Artists,
                    metadataDto.Album,
                    albumId,
                    metadataDto.ReleaseYear,
                    metadataDto.ReleaseDateYYYYMMDD == null
                        ? null
                        : new(int.Parse(metadataDto.ReleaseDateYYYYMMDD.AsSpan(0, 4)),
                              int.Parse(metadataDto.ReleaseDateYYYYMMDD.AsSpan(4, 2)),
                              int.Parse(metadataDto.ReleaseDateYYYYMMDD.AsSpan(6, 2))),
                    trackNumber,
                    trackCount,
                    preferredThumbnail.Url),
                metadataDto.Filename);
    }

    private async Task<string> GetAlbumIdAsync(string browseId)
    {
        var browsePageData = await youTubePageService.GetPageInitialDataAsync($"https://music.youtube.com/browse/{browseId}");
        var infoPage = browsePageData[1];
        var contents = infoPage["contents"];

        if (contents == null)
        {
            throw new InvalidOperationException("Unable to get album id. You may need to refresh your cookies.txt.");
        }

        var playlistParent =
            contents["twoColumnBrowseResultsRenderer"]["secondaryContents"]["musicResponsiveListItemRenderer"]?["overlay"]["musicItemThumbnailOverlayRenderer"]["content"]["musicPlayButtonRenderer"]["playNavigationEndpoint"]["watchEndpoint"] ??
            contents["twoColumnBrowseResultsRenderer"]["secondaryContents"]["sectionListRenderer"]["contents"][0]["musicShelfRenderer"]["contents"].Select(x => x["musicResponsiveListItemRenderer"]["overlay"]["musicItemThumbnailOverlayRenderer"]["content"]["musicPlayButtonRenderer"]["playNavigationEndpoint"]).FirstOrDefault(x => x != null)["watchEndpoint"];

        var playlistId = playlistParent.Get<string>("playlistId");
        return playlistId;
    }
}
