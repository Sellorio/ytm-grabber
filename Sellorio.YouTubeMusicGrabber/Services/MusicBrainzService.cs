using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    private const int MaxPagesToSearch = 10;
    private const int DelayBetweenCalls = 1100;

    private static readonly JsonSerializerOptions _jsonOptions;

    static MusicBrainzService()
    {
        _jsonOptions = new JsonSerializerOptions();
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
    }

    public async Task<RecordingMatch> FindRecordingAsync(string album, string[] artists, string title, DateOnly releaseDate, int albumTrackCount, bool promptForIdIfNotFound = false)
    {
        var recording = await SearchForRecordingAsync(album, artists, title);

        if (recording == null)
        {
            if (!promptForIdIfNotFound)
            {
                return null;
            }

            var position = Console.GetCursorPosition();

            Console.Write(
                $"\r\n" +
                $"Unable to find MusicBrainz Recording that matches the following details:\r\n" +
                $"  Title:   {title}\r\n" +
                $"  Album:   {album}\r\n" +
                $"  Artists: {string.Join(";", artists)}\r\n" +
                $"\r\n" +
                $"Please manually search and enter the recording's ID or press enter to cancel.\r\n" +
                $"MusicBrainz ID: ");

            var musicBrainzIdString = Console.ReadLine();

            if (string.IsNullOrEmpty(musicBrainzIdString))
            {
                return null;
            }

            if (!Guid.TryParse(musicBrainzIdString, out var musicBrainzId))
            {
                throw new InvalidOperationException("Invalid MusicBrainz ID: must be a valid GUID.");
            }

            recording = await GetRecordingByIdAsync(musicBrainzId);

            if (recording == null)
            {
                throw new InvalidOperationException("MusicBrainz ID does not exist.");
            }

            ConsoleHelper.ResetBackToPositionAndClearConsole(position);
            Console.WriteLine($"Using manually selected MusicBrainz Recording {musicBrainzId}...");
        }

        var release =
            recording.Releases.FirstOrDefault(x =>
                CompareHelper.ToSearchNormalisedTitle(x.Title) == CompareHelper.ToSearchNormalisedTitle(album) &&
                x.Date == releaseDate &&
                x.Media.Any(y => y.Format is "Digital Media" or "CD" && y.TrackCount == albumTrackCount))
                    ?? throw new InvalidOperationException("Unable to match metadata with MusicBrainz Release.");

        var medium = release.Media.First(x => x.Format is "Digital Media" or "CD" && x.TrackCount == albumTrackCount);
        var tracks = medium.Track ?? medium.Tracks;
        var track = tracks.First(x => CompareHelper.ToSearchNormalisedTitle(x.Title) == CompareHelper.ToSearchNormalisedTitle(recording.Title));

        return new RecordingMatch
        {
            Recording = recording,
            Release = release,
            ReleaseGroup = release.ReleaseGroup,
            Medium = medium,
            Track = track
        };
    }

    private async Task<Recording> GetRecordingByIdAsync(Guid recordingId)
    {
        var result = await httpClient.GetAsync($"https://musicbrainz.org/ws/2/recording/{recordingId}?fmt=json&inc=releases+artists+release-groups+media");

        if (result.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        result.EnsureSuccessStatusCode();

        var json = await result.Content.ReadAsStringAsync();
        var recording = JsonSerializer.Deserialize<Recording>(json, _jsonOptions);

        return recording;
    }

    private async Task<Recording> SearchForRecordingAsync(string album, string[] artists, string title)
    {
        var normalisedAlbum = CompareHelper.ToSearchNormalisedTitle(album);
        var normalisedArtists = artists.Select(CompareHelper.ToSearchNormalisedName).ToArray();
        var normalisedTitle = CompareHelper.ToSearchNormalisedTitle(title);

        var featuredArtist = CompareHelper.ExtractFeaturingCredit(title, out int featuredArtistCreditIndex);
        var normalisedFeaturedArtist = featuredArtist == null ? null : CompareHelper.ToSearchNormalisedName(featuredArtist);
        var titleWithoutFeaturedArtist = featuredArtist == null ? title : title.Substring(0, featuredArtistCreditIndex);

        Recording recording;

        var offset = 0;
        var query = new RecordSearchQuery { Album = album, Artists = artists, Title = titleWithoutFeaturedArtist };
        var searchResult = await DoSearchAsync(query, offset);

        if (searchResult.Count == 0)
        {
            // try getting results without artists filter
            query.Artists = null;
            await Task.Delay(DelayBetweenCalls);
            searchResult = await DoSearchAsync(query, offset);
        }

        if (searchResult.Count == 0)
        {
            // try getting results without album and artists filters
            query.Album = null;
            await Task.Delay(DelayBetweenCalls);
            searchResult = await DoSearchAsync(query, offset);
        }

        if (searchResult.Count == 0)
        {
            // rip we tried so hard to get something... anything
            return null;
        }

        while (true)
        {
            recording = FindPerfectlyMatchedRecording(searchResult, normalisedAlbum, normalisedArtists, normalisedTitle, normalisedFeaturedArtist);

            if (recording != null)
            {
                return recording;
            }

            if (offset >= MaxPageSize * MaxPagesToSearch || offset >= searchResult.Count)
            {
                break;
            }

            await Task.Delay(DelayBetweenCalls);
            offset += MaxPageSize;
            searchResult = await DoSearchAsync(query, offset);
        }

        return null;
    }

    private async Task<RecordingsSearchResult> DoSearchAsync(RecordSearchQuery query, int offset)
    {
        var queryText = BuildQuery(query.Album, query.Artists, query.Title);
        var searchUri = $"recording/?fmt=json&limit={MaxPageSize}&offset={offset}&query={Uri.EscapeDataString(queryText)}";
        var response = await httpClient.GetAsync(searchUri);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var searchResult = JsonSerializer.Deserialize<RecordingsSearchResult>(json, _jsonOptions);
        return searchResult;
    }

    private static string BuildQuery(string album, string[] artists, string title)
    {
        var searchClauses = new List<(string Property, string Value)>
        {
            ("recording", title)
        };

        if (album != null)
        {
            searchClauses.Add(("release", album));
        }

        if (artists != null)
        {
            foreach (var artist in artists)
            {
                if (!artist.Contains('(') && !artist.Contains(',') && !artist.Contains(';'))
                {
                    searchClauses.Add(("artist", artist));
                }
            }
        }

        var query =
            string.Join(
                " AND ",
                searchClauses.Select(x => $"{x.Property}:\"{EscapeMusicBrainzQueryValue(x.Value)}\""));

        return query;
    }

    private static string EscapeMusicBrainzQueryValue(string input)
    {
        return MusicBrainzEscapeCharactersRegex().Replace(input, @"\$1");
    }

    private static Recording FindPerfectlyMatchedRecording(RecordingsSearchResult searchResult, string album, string[] artists, string title, string featuredArtist)
    {
        foreach (var recording in searchResult.Recordings)
        {
            if (CompareHelper.ToSearchNormalisedTitle(recording.Title) != title)
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

            return recording;
        }

        return null;
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
