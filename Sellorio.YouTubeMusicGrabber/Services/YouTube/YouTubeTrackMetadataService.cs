using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Exceptions;
using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;
using Sellorio.YouTubeMusicGrabber.Services.CoverArtArchive;
using Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube;

internal partial class YouTubeTrackMetadataService(
    IYouTubeDlpService youTubeDlpService,
    IYouTubeXhrService youTubeXhrService,
    IYouTubePageService youTubePageService,
    IYouTubeAlbumMetadataService youTubeAlbumMetadataService,
    IMusicBrainzService musicBrainzService,
    ICoverArtArchiveService coverArtArchiveService) : IYouTubeTrackMetadataService
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
                var initialEndpoint = pageData[0].Get<string>("INITIAL_ENDPOINT");
                var videoId = JsonNavigator.FromString(initialEndpoint)["watchEndpoint"].Get<string>("videoId");

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

    public async Task<YouTubeVideoMetadata> GetMetadataAsync(string youTubeId)
    {
        var metadataDto = await youTubeDlpService.GetTrackMetadataAsync(youTubeId);
        var nextData = await youTubeXhrService.GetNextAsync(youTubeId);

        var bestThumbnailPreferenceScore =
            metadataDto.Thumbnails != null && metadataDto.Thumbnails.Any()
                ? metadataDto.Thumbnails.Min(x => x.Preference)
                : 0;

        var preferredThumbnail =
            metadataDto.Thumbnails
                .Where(x => x.Preference == bestThumbnailPreferenceScore && x.Width == x.Height && x.Width < 500)
                .OrderByDescending(x => x.Width)
                .FirstOrDefault();

        var result = new YouTubeVideoMetadata
        {
            MusicMetadata = metadataDto.Album == null ? null : new()
            {
                Title = metadataDto.Title,
                Album = metadataDto.Album,
                Artists = metadataDto.Artists,
                AlternateTitle = metadataDto.AlternateTitle,
                ReleaseDate =
                metadataDto.ReleaseDateYYYYMMDD == null
                    ? null
                    : new(int.Parse(metadataDto.ReleaseDateYYYYMMDD.AsSpan(0, 4)),
                          int.Parse(metadataDto.ReleaseDateYYYYMMDD.AsSpan(4, 2)),
                          int.Parse(metadataDto.ReleaseDateYYYYMMDD.AsSpan(6, 2))),
                ReleaseYear = metadataDto.ReleaseYear,
                AlbumArtUrl = preferredThumbnail?.Url
            },
            Id = metadataDto.Id,
            Thumbnails = metadataDto.Thumbnails,
            Filename = metadataDto.Filename
        };

        if (result.MusicMetadata == null)
        {
            result.MusicMetadata = await PromptUserForMusicMetadataAsync(youTubeId);
            return result;
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
                result.MusicMetadata.Title = title1;
                result.MusicMetadata.AlternateTitle = title2;
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

        if (result.MusicMetadata.Artists == null)
        {
            result.MusicMetadata.Artists = [];

            for (var i = 0; i < byLineSections.ArrayLength - 3; i += 2)
            {
                var artistName = byLineSections[i].Get<string>("text");
                result.MusicMetadata.Artists.Add(artistName);
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

        result.MusicMetadata.Album = albumName;
        result.MusicMetadata.AlbumId = await GetAlbumIdAsync(albumBrowseId);

        var albumMetadata = await youTubeAlbumMetadataService.GetMetadataAsync(result.MusicMetadata.AlbumId);

        result.MusicMetadata.TrackCount = albumMetadata.Tracks.Count;
        result.MusicMetadata.TrackNumber = albumMetadata.Tracks.Index().First(x => x.Item.Id == youTubeId).Index + 1;

        return result;
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

    private async Task<YouTubeMusicMetadata> PromptUserForMusicMetadataAsync(string youTubeId)
    {
        ConsoleHelper.Write($"{youTubeId} is a non-music video file.\r\nDo you want to enter a MusicBrainz Track URL to use as the metadata source? (Y/n): ", ConsoleColor.White);
        var key = Console.ReadKey();

        if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Y)
        {
            ConsoleHelper.Write("\r\nMusicBrainz Track URL: ", ConsoleColor.White);
            var trackIdOrUrl = Console.ReadLine();

            if (string.IsNullOrEmpty(trackIdOrUrl))
            {
                return await PromptUserForMusicMetadataAsync(youTubeId);
            }

            if (!Uri.TryCreate(trackIdOrUrl, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException("Invalid MusicBrainz URL: must be a valid URL.");
            }

            var matchUrlFormat = Regex.Match(uri.LocalPath + uri.Fragment, @"^\/release\/([a-zA-Z0-9-]+)\/[a-zA-Z0-9]+/[a-zA-Z0-9-]+#([a-zA-Z0-9-]+)$");

            if (!matchUrlFormat.Success)
            {
                throw new InvalidOperationException("Not a valid MusicBrainz Track URL: must be the URL to a track.");
            }

            var releaseId = Guid.Parse(matchUrlFormat.Groups[1].Value);
            var trackId = Guid.Parse(matchUrlFormat.Groups[2].Value);

            var release = await musicBrainzService.GetReleaseByIdAsync(releaseId);
            var track = release.Media.SelectMany(x => x.Tracks).First(x => x.Id == trackId);

            var releaseArt = await coverArtArchiveService.GetReleaseArtAsync(releaseId);
            var releaseArtUrl = releaseArt?.Images.FirstOrDefault(x => x.Front)?.Thumbnails["500"];

            if (releaseArtUrl == null)
            {
                releaseArtUrl =
                    ConsoleHelper.PromptForUri(
                        "This MusicBrainz release does not have album art.\r\n" +
                        "Please enter a URL to the album art you want to use.\r\n" +
                        "The art must be square and no more than 500x500.\r\n" +
                        "Album Art URL: ")?.AbsoluteUri;
            }

            return new YouTubeMusicMetadata
            {
                Title = track.Title,
                Album = release.Title,
                AlbumArtUrl = releaseArtUrl,
                Artists = release.ArtistCredit.Select(x => x.Name).ToArray(),
                ReleaseDate = release.Date,
                ReleaseYear = release.ReleaseYear,
                TrackCount = release.TrackCount,
                TrackNumber = musicBrainzService.GetTrackNumberWithoutAdornments(track.Number)
            };
        }
        else if (key.Key == ConsoleKey.N)
        {
            ConsoleHelper.Write($"Do you want to enter details manually? (Y/n): ", ConsoleColor.White);
            key = Console.ReadKey();

            if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Y)
            {
                ConsoleHelper.Write("\r\n1/7 Title: ", ConsoleColor.White);
                var title = Console.ReadLine();

                if (string.IsNullOrEmpty(title))
                {
                    ConsoleHelper.WriteLine("Title is required.", ConsoleColor.Red);
                    ConsoleHelper.Write("\r\n1/7 Title: ", ConsoleColor.White);
                    title = Console.ReadLine();

                    if (string.IsNullOrEmpty(title))
                    {
                        return null;
                    }
                }

                ConsoleHelper.Write("2/7 Album: ", ConsoleColor.White);
                var album = Console.ReadLine();

                if (string.IsNullOrEmpty(album))
                {
                    ConsoleHelper.WriteLine("Album is required.", ConsoleColor.Red);
                    ConsoleHelper.Write("\r\n2/7 Album: ", ConsoleColor.White);
                    album = Console.ReadLine();

                    if (string.IsNullOrEmpty(album))
                    {
                        return null;
                    }
                }

                ConsoleHelper.Write("3/7 Alternate Title (): ", ConsoleColor.White);
                var alternateTitle = Console.ReadLine();

                ConsoleHelper.Write("4/7 Artists ; separated (): ", ConsoleColor.White);
                var artistsRaw = Console.ReadLine();

                ConsoleHelper.Write("5/7 Release Year (): ", ConsoleColor.White);
                var releaseYearString = Console.ReadLine();
                int? releaseYear = null;

                if (!string.IsNullOrEmpty(releaseYearString))
                {
                }    
                else if (!int.TryParse(releaseYearString, out var releaseYearValue))
                {
                    ConsoleHelper.WriteLine("Track number must be an integer.", ConsoleColor.Red);
                    ConsoleHelper.Write("\r\n6/7 Track Number (1): ", ConsoleColor.White);
                    releaseYearString = Console.ReadLine();

                    if (!string.IsNullOrEmpty(releaseYearString))
                    {
                    }
                    else if (!int.TryParse(releaseYearString, out releaseYearValue))
                    {
                        return null;
                    }
                    else
                    {
                        releaseYear = releaseYearValue;
                    }
                }
                else
                {
                    releaseYear = releaseYearValue;
                }

                ConsoleHelper.Write("6/7 Track Number (1): ", ConsoleColor.White);
                var trackNumberString = Console.ReadLine();

                if (string.IsNullOrEmpty(trackNumberString))
                {
                    trackNumberString = "1";
                }

                if (!int.TryParse(trackNumberString, out var trackNumber))
                {
                    ConsoleHelper.WriteLine("Track number must be an integer.", ConsoleColor.Red);
                    ConsoleHelper.Write("\r\n6/7 Track Number (1): ", ConsoleColor.White);
                    trackNumberString = Console.ReadLine();

                    if (string.IsNullOrEmpty(trackNumberString))
                    {
                        trackNumberString = "1";
                    }

                    if (!int.TryParse(trackNumberString, out trackNumber))
                    {
                        return null;
                    }
                }

                ConsoleHelper.Write("7/7 Track Count (1): ", ConsoleColor.White);
                var trackCountString = Console.ReadLine();

                if (!int.TryParse(trackCountString, out var trackCount))
                {
                    ConsoleHelper.WriteLine("Track number must be an integer.", ConsoleColor.Red);
                    ConsoleHelper.Write("\r\n7/7 Track Number (1): ", ConsoleColor.White);
                    trackCountString = Console.ReadLine();

                    if (string.IsNullOrEmpty(trackCountString))
                    {
                        trackCountString = "1";
                    }

                    if (!int.TryParse(trackCountString, out trackCount))
                    {
                        return null;
                    }
                }

                var releaseArtUrl =
                    ConsoleHelper.PromptForUri(
                        "Please enter a URL to the album art you want to use.\r\n" +
                        "The art must be square and no more than 500x500.\r\n" +
                        "Album Art URL: ")?.AbsoluteUri;

                return new YouTubeMusicMetadata
                {
                    Title = title,
                    TrackNumber = trackNumber,
                    TrackCount = trackCount,
                    Album = album,
                    AlternateTitle = alternateTitle,
                    Artists = artistsRaw.Split(';').Select(x => x.Trim()).ToArray(),
                    ReleaseYear = releaseYear,
                    AlbumArtUrl = releaseArtUrl
                };
            }
            else if (key.Key == ConsoleKey.N)
            {
                return null;
            }
            else
            {
                ConsoleHelper.WriteLine(string.Empty, ConsoleColor.White);
                return await PromptUserForMusicMetadataAsync(youTubeId);
            }
        }
        else
        {
            ConsoleHelper.WriteLine(string.Empty, ConsoleColor.White);
            return await PromptUserForMusicMetadataAsync(youTubeId);
        }
    }
}
