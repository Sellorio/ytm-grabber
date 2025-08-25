using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal partial class MusicBrainzService(HttpClient httpClient) : IMusicBrainzService
{
    private const int MaxPageSize = 100;
    private const int MaxPagesToSearch = 4;
    private const int DelayBetweenCalls = 1100;

    private static readonly JsonSerializerOptions _jsonOptions;

    static MusicBrainzService()
    {
        _jsonOptions = new JsonSerializerOptions();
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
    }

    public async Task<RecordingMatch> FindRecordingAsync(string album, IList<string> artists, IList<string> possibleTitles, DateOnly? releaseDate, int? releaseYear, int albumTrackCount, bool promptForIdIfNotFound = false)
    {
        var recording = await SearchForRecordingAsync(album, artists, possibleTitles, albumTrackCount, releaseDate, releaseYear);

        if (recording == null)
        {
            if (!promptForIdIfNotFound)
            {
                return null;
            }

            ConsoleHelper.Write(
                $"\r\n" +
                $"Unable to find MusicBrainz Recording that matches the following details:\r\n" +
                $"  Title:   {string.Join("/", possibleTitles.Distinct())}\r\n" +
                $"  Album:   {album}\r\n" +
                $"  Artists: {string.Join(";", artists ?? ["<<Unknown>>"])}\r\n" +
                $"\r\n" +
                $"Please manually search and enter the recording's ID or URL or press enter to cancel.\r\n" +
                $"ID/URL: ",
                ConsoleColor.White);

            var musicBrainzIdString = Console.ReadLine();
            Console.WriteLine();

            if (string.IsNullOrEmpty(musicBrainzIdString))
            {
                return null;
            }

            if (!Guid.TryParse(musicBrainzIdString, out var musicBrainzId) &&
                    (!Uri.TryCreate(musicBrainzIdString, UriKind.Absolute, out var uri) ||
                    !Guid.TryParse(Regex.Match(uri.LocalPath, @"^\/recording\/([a-zA-Z0-9-]+)$").Groups[1].Value, out musicBrainzId)))
            {
                throw new InvalidOperationException("Invalid MusicBrainz ID: must be a valid GUID or URL.");
            }

            recording = await GetRecordingByIdAsync(musicBrainzId);

            if (recording == null)
            {
                throw new InvalidOperationException("MusicBrainz ID does not exist.");
            }
        }

        await PopulateMediaAndTrackCountAndAdjustTrackOffsetsAsync(recording.Releases);

        var release = await GetMatchingReleaseAsync(recording, album, releaseDate, releaseYear, albumTrackCount);

        if (release == null)
        {
            return null;
        }

        Medium medium = null;
        Track track = null;

        foreach (var releaseMedium in release.Media)
        {
            if (releaseMedium.Track != null)
            {
                medium = releaseMedium;
                track = releaseMedium.Track[0];
                break;
            }
            else if (releaseMedium.Tracks != null)
            {
                foreach (var releaseTrack in releaseMedium.Tracks)
                {
                    if (releaseTrack.Recording.Id == recording.Id)
                    {
                        medium = releaseMedium;
                        track = releaseTrack;
                        break;
                    }
                }

                if (track != null)
                {
                    break;
                }
            }
        }

        if (track == null)
        {
            throw new InvalidOperationException("Unable to find track.");
        }

        return new RecordingMatch
        {
            Recording = recording,
            Release = release,
            ReleaseGroup = release.ReleaseGroup,
            Medium = medium,
            Track = track
        };
    }

    public async Task<Recording> GetRecordingByIdAsync(Guid recordingId)
    {
        var result = await httpClient.GetAsync($"recording/{recordingId}?fmt=json&inc=releases+artists+release-groups+media");

        if (result.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        result.EnsureSuccessStatusCode();

        var json = await result.Content.ReadAsStringAsync();
        var recording = JsonSerializer.Deserialize<Recording>(json, _jsonOptions);

        foreach (var release in recording.Releases)
        {
            foreach (var media in release.Media)
            {
                // establishing consistency in MusicBrainz API - Track will be used when a single, matching track
                // is returned, tracks will only be used if all tracks are returned
                media.Track = media.Tracks;
            }
        }

        return recording;
    }

    public async Task<Release> GetReleaseByIdAsync(Guid releaseId)
    {
        var result = await httpClient.GetAsync($"release/{releaseId}?fmt=json&inc=artists+release-groups+media+recordings");

        if (result.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        result.EnsureSuccessStatusCode();

        var json = await result.Content.ReadAsStringAsync();
        var release = JsonSerializer.Deserialize<Release>(json, _jsonOptions);

        var trackOffset = 0;

        foreach (var medium in release.Media)
        {
            medium.TrackOffset = trackOffset;

            foreach (var track in medium.Tracks)
            {
                track.Number = (int.Parse(track.Number) + trackOffset).ToString();
            }

            trackOffset += medium.TrackCount;
        }

        release.TrackCount = trackOffset;

        return release;
    }

    private async Task<Recording> SearchForRecordingAsync(string album, IList<string> artists, IList<string> possibleTitles, int albumTrackCount, DateOnly? releaseDate, int? releaseYear)
    {
        if (artists == null)
        {
            return null;
        }

        var normalisedAlbum = CompareHelper.ToSearchNormalisedTitle(album);
        var normalisedArtists = artists.Select(CompareHelper.ToSearchNormalisedName).ToArray();
        var normalisedTitles = possibleTitles.Select(CompareHelper.ToSearchNormalisedTitle).ToArray();

        var titlesWithoutFeaturedArtist =
            possibleTitles
                .Select(x =>
                    CompareHelper.ExtractFeaturingCredit(x, out var featuredArtistCreditIndex) != null
                        ? x.Substring(0, featuredArtistCreditIndex)
                        : x)
                .ToArray();

        Recording recording;

        var offset = 0;
        var query = new RecordSearchQuery { Album = album, Artists = artists, PossibleTitles = titlesWithoutFeaturedArtist };

        ConsoleHelper.WriteLine("Searching by Title, Album and Artists...", ConsoleColor.DarkGray);
        var searchResult = await DoSearchAsync(query, offset);

        if (searchResult.Count == 0)
        {
            // try getting results without artists filter
            query.Artists = null;
            await WaitBetweenCallsAsync();
            ConsoleHelper.WriteLine("Searching by Title and Album...", ConsoleColor.DarkGray);
            searchResult = await DoSearchAsync(query, offset);
        }

        if (searchResult.Count == 0)
        {
            // try getting results without album and artists filters
            query.Album = null;
            await WaitBetweenCallsAsync();
            ConsoleHelper.WriteLine("Searching by only Title...", ConsoleColor.DarkGray);
            searchResult = await DoSearchAsync(query, offset);
        }

        if (searchResult.Count == 0)
        {
            // rip we tried so hard to get something... anything
            return null;
        }

        while (true)
        {
            recording = await MatchRecordingAsync(searchResult, normalisedAlbum, normalisedArtists, normalisedTitles, albumTrackCount);

            if (recording != null)
            {
                return recording;
            }

            if (offset >= MaxPageSize * MaxPagesToSearch || offset >= searchResult.Count)
            {
                break;
            }

            await WaitBetweenCallsAsync();
            offset += MaxPageSize;
            searchResult = await DoSearchAsync(query, offset);
        }

        return null;
    }
    private static async Task WaitBetweenCallsAsync()
    {
        ConsoleHelper.Write("Rate limiting wait...  ", ConsoleColor.DarkGray);
        await Task.Delay(DelayBetweenCalls);
        ConsoleHelper.WriteLine("Continuing.", ConsoleColor.DarkGray);
    }

    private async Task<RecordingsSearchResult> DoSearchAsync(RecordSearchQuery query, int offset)
    {
        var queryText = BuildQuery(query.Album, query.Artists, query.PossibleTitles);
        var searchUri = $"recording/?fmt=json&limit={MaxPageSize}&offset={offset}&query={Uri.EscapeDataString(queryText)}";
        var response = await httpClient.GetAsync(searchUri);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var searchResult = JsonSerializer.Deserialize<RecordingsSearchResult>(json, _jsonOptions);

        foreach (var recording in searchResult.Recordings)
        {
            if (recording.Releases != null)
            {
                foreach (var release in recording.Releases)
                {
                    // Ok strap in for this one.
                    // This field is SOMETIMES set by MusicBrainz but usually not.
                    // I'm using this field to decide whether or not to run the logic in
                    // PopulateMediaAndTrackCountAndAdjustTrackOffsetsAsync so I'm making
                    // it consistently 0 so that logic can run without adding any extra state tracking.
                    release.TrackCount = 0;
                }
            }
        }

        return searchResult;
    }

    private static string BuildQuery(string album, IList<string> artists, IList<string> possibleTitles)
    {
        var result = new StringBuilder(200);

        var isFirstTitle = true;

        if (possibleTitles.Count > 1)
        {
            result.Append('(');
        }

        foreach (var title in possibleTitles)
        {
            if (!isFirstTitle)
            {
                result.Append(" OR ");
            }

            result.Append("recording:\"").Append(EscapeMusicBrainzQueryValue(title)).Append('"');
            isFirstTitle = false;
        }

        if (possibleTitles.Count > 1)
        {
            result.Append(')');
        }

        if (album != null)
        {
            result.Append(" AND ");

            if (album.Contains(' '))
            {
                // I don't like this but it works for one specific edge case so I'll do it anyway
                result
                    .Append("(release:\"")
                    .Append(EscapeMusicBrainzQueryValue(album))
                    .Append("\" OR release:\"")
                    .Append(EscapeMusicBrainzQueryValue(album.Replace(" ", "")))
                    .Append("\")");
            }
            else
            {
                result
                    .Append("release:\"")
                    .Append(EscapeMusicBrainzQueryValue(album))
                    .Append('"');
            }
        }

        if (artists != null)
        {
            foreach (var artist in artists)
            {
                if (!artist.Contains('(') && !artist.Contains(',') && !artist.Contains(';'))
                {
                    result
                        .Append(" AND artist:\"")
                        .Append(EscapeMusicBrainzQueryValue(artist))
                        .Append('"');
                }
            }
        }

        return result.ToString();
    }

    private async Task<Release> GetMatchingReleaseAsync(Recording recording, string album, DateOnly? releaseDate, int? releaseYear, int albumTrackCount)
    {
        var releases1 =
            recording.Releases
                .Where(x =>
                    x.TrackCount == albumTrackCount &&
                    x.Media.All(y => y.Format is "Digital Media" or "CD"))
                .ToArray();

        var releases2 =
            releases1
                .Where(x => CompareHelper.ToSearchNormalisedTitle(x.Title) == CompareHelper.ToSearchNormalisedTitle(album))
                .ToArray();

        if (releases2.Length == 0)
        {
            releases2 = releases1;
        }

        var releases3 = releases2;

        if (releaseDate != null)
        {
            releases3 = releases2.Where(x => x.Date != null && x.Date == releaseDate).ToArray();

            if (releases3.Length == 0)
            {
                releases3 = releases2.OrderBy(x => x.Date == null ? 9999 : Math.Abs((releaseDate.Value.ToDateTime(default) - x.Date.Value.ToDateTime(default)).Days)).ToArray();
            }
        }
        else if (releaseYear != null)
        {
            releases3 = releases2.Where(x => x.ReleaseYear == releaseYear).ToArray();

            if (releases3.Length == 0)
            {
                releases3 = releases2.OrderBy(x => x.ReleaseYear == null ? 9999 : Math.Abs(x.ReleaseYear.Value - releaseYear.Value)).ToArray();
            }
        }

        var result = releases3;

        if (result.Length == 0)
        {
            ConsoleHelper.Write(
                $"\r\n" +
                $"Unable to find MusicBrainz Release (Album) that matches the following details:\r\n" +
                $"  Album:   {album}\r\n" +
                $"  Title:   {string.Join("/", recording.Title)}\r\n" +
                $"  Artists: {string.Join(";", recording.ArtistCredit.Select(x => x.Name))}\r\n" +
                $"\r\n" +
                $"Please manually search and enter the recording's ID or URL or press enter to cancel.\r\n" +
                $"ID/URL: ",
                ConsoleColor.White);

            var musicBrainzIdString = Console.ReadLine();
            Console.WriteLine();

            if (string.IsNullOrEmpty(musicBrainzIdString))
            {
                return null;
            }

            if (!Guid.TryParse(musicBrainzIdString, out var musicBrainzId) &&
                    (!Uri.TryCreate(musicBrainzIdString, UriKind.Absolute, out var uri) ||
                    !Guid.TryParse(Regex.Match(uri.LocalPath, @"^\/release\/([a-zA-Z0-9-]+)$").Groups[1].Value, out musicBrainzId)))
            {
                throw new InvalidOperationException("Invalid MusicBrainz ID: must be a valid GUID or URL.");
            }

            var release = await GetReleaseByIdAsync(musicBrainzId);

            if (release == null)
            {
                throw new InvalidOperationException("MusicBrainz ID does not exist.");
            }
            else if (release.Media.All(x => x.Tracks.All(y => y.Recording.Id != recording.Id)))
            {
                throw new InvalidOperationException("Recording not found in provided release.");
            }

            return release;
        }

        return result[0];
    }

    private static string EscapeMusicBrainzQueryValue(string input)
    {
        return MusicBrainzEscapeCharactersRegex().Replace(input, @"\$1");
    }

    private async Task<Recording> MatchRecordingAsync(RecordingsSearchResult searchResult, string album, string[] artists, string[] possibleTitles, int albumTrackCount)
    {
        foreach (var recording in searchResult.Recordings)
        {
            if (possibleTitles.All(x => x != CompareHelper.ToSearchNormalisedTitle(recording.Title)))
            {
                continue;
            }

            if (recording.Releases == null ||
                recording.Releases.All(x => CompareHelper.ToSearchNormalisedTitle(x.Title) != album))
            {
                continue;
            }

            if (recording.ArtistCredit.Any(x => !ContainsArtist(artists, x.Artist)))
            {
                continue;
            }

            await PopulateMediaAndTrackCountAndAdjustTrackOffsetsAsync(recording.Releases);

            if (recording.Releases.All(x =>
                x.Media.All(x => x.Format is not "Digital Media" and not "CD") ||
                x.TrackCount != albumTrackCount))
            {
                continue;
            }

            return recording;
        }

        return null;
    }
    
    private async Task PopulateMediaAndTrackCountAndAdjustTrackOffsetsAsync(IList<Release> releases)
    {
        var releasesWithoutTrackCount = releases.Where(x => x.TrackCount == 0 && x.Media.All(y => y.Format is "Digital Media" or "CD")).ToArray();

        if (releasesWithoutTrackCount.Length == 0)
        {
            return;
        }

        await WaitBetweenCallsAsync();

        var query = "reid:(" + string.Join(" OR ", releasesWithoutTrackCount.Select(x => $"\"{x.Id}\"")) + ")";
        var resposne = await httpClient.GetAsync("release?fmt=json&query=" + Uri.EscapeDataString(query));
        var json = await resposne.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<ReleasesSearchResult>(json, _jsonOptions);

        foreach (var searchResultRelease in results.Releases)
        {
            var release = releasesWithoutTrackCount.First(x => x.Id == searchResultRelease.Id);
            release.TrackCount = searchResultRelease.TrackCount;

            var trackOffset = 0;

            foreach (var searchResultMedium in searchResultRelease.Media)
            {
                var releaseMedium = release.Media.FirstOrDefault(x => x.Id == searchResultMedium.Id);

                searchResultMedium.TrackOffset = trackOffset;

                if (releaseMedium != null)
                {
                    searchResultMedium.Track = releaseMedium.Track;
                    searchResultMedium.Tracks = releaseMedium.Tracks;

                    if (searchResultMedium.Track != null)
                    {
                        searchResultMedium.Track[0].Number = (GetTrackNumberWithoutAdornments(searchResultMedium.Track[0].Number) + trackOffset).ToString();
                    }
                    else if (searchResultMedium.Tracks != null)
                    {
                        foreach (var track in searchResultMedium.Tracks)
                        {
                            track.Number = (GetTrackNumberWithoutAdornments(track.Number) + trackOffset).ToString();
                        }
                    }
                }

                trackOffset += searchResultMedium.TrackCount;
            }

            release.Media = searchResultRelease.Media;
        }
    }

    public int GetTrackNumberWithoutAdornments(string trackNumber)
    {
        // just get the number from the following formats:
        // 1-17 (disk 1, track 17)
        // A3 (side A, track 3)
        var match = Regex.Match(trackNumber, @"^(?:[a-zA-Z]+|[0-9]+\-|)([0-9]+)$");

        if (!match.Success)
        {
            throw new InvalidOperationException("Unable to parse track number.");
        }

        return int.Parse(match.Groups[1].Value);
    }

    private static bool ContainsArtist(string[] artists, Artist artist)
    {
        foreach (var searchArtist in artists)
        {
            if (ContainsName(searchArtist, artist.Name) ||
                artist.Aliases != null && artist.Aliases.Any(x => ContainsName(searchArtist, x.Name)) ||
                ContainsName(searchArtist, artist.SortName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsName(string artistText, string name)
    {
        name = CompareHelper.ToSearchNormalisedName(name);

        if (artistText.Contains(name))
        {
            return true;
        }

        var indexOfComma = name.IndexOf(',');

        if (indexOfComma == -1)
        {
            return false;
        }

        var firstName = name.Substring(indexOfComma + 2);
        var lastName = name.Substring(0, indexOfComma);

        return artistText.Contains($"{firstName} {lastName}") || artistText.Contains($"{lastName} {firstName}");
    }

    [GeneratedRegex(@"(\+|\-|\&\&|\|\||\!|\(|\)|\{|\}|\[|\]|\^|\""|\~|\*|\?|\:|\\|\/)")]
    private static partial Regex MusicBrainzEscapeCharactersRegex();
}
