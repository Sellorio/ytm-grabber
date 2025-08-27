using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Models;
using Sellorio.YouTubeMusicGrabber.Services.CoverArtArchive;

namespace Sellorio.YouTubeMusicGrabber.Services;
internal class FallbackMetadataService(
    IMusicBrainzService musicBrainzService,
    ICoverArtArchiveService coverArtArchiveService) : IFallbackMetadataService
{
    public async Task<ItemMusicMetadata> PromptUserForMusicMetadataAsync()
    {
        ConsoleHelper.Write($"Do you want to enter a MusicBrainz Track URL to use as the metadata source? (Y/n): ", ConsoleColor.White);
        var key = Console.ReadKey();

        if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Y)
        {
            ConsoleHelper.Write(
                "\r\n" +
                "Example: https://musicbrainz.org/release/00000000-0000-0000-0000-000000000000/disc/0#00000000-0000-0000-0000-000000000000\r\n" +
                "MusicBrainz Track URL: ", ConsoleColor.White);

            var trackIdOrUrl = Console.ReadLine();

            if (string.IsNullOrEmpty(trackIdOrUrl))
            {
                return await PromptUserForMusicMetadataAsync();
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

            return new ItemMusicMetadata(
                track.Title,
                null,
                release.ArtistCredit.Select(x => x.Name).ToArray(),
                release.Title,
                null,
                release.ReleaseYear,
                release.Date,
                musicBrainzService.GetTrackNumberWithoutAdornments(track.Number),
                release.TrackCount,
                releaseArtUrl);
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


                return new ItemMusicMetadata(
                    title,
                    alternateTitle,
                    artistsRaw.Split(';').Select(x => x.Trim()).ToArray(),
                    album,
                    null,
                    releaseYear,
                    null,
                    trackNumber,
                    trackCount,
                    releaseArtUrl);
            }
            else if (key.Key == ConsoleKey.N)
            {
                return null;
            }
            else
            {
                ConsoleHelper.WriteLine(string.Empty, ConsoleColor.White);
                return await PromptUserForMusicMetadataAsync();
            }
        }
        else
        {
            ConsoleHelper.WriteLine(string.Empty, ConsoleColor.White);
            return await PromptUserForMusicMetadataAsync();
        }
    }
}
