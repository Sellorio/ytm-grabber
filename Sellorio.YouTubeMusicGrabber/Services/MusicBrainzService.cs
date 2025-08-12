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

    private static readonly JsonSerializerOptions _jsonOptions;

    static MusicBrainzService()
    {
        _jsonOptions = new JsonSerializerOptions();
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
    }

    public async Task<RecordingMatch> FindRecordingAsync(string album, string artist, string title, DateOnly releaseDate, int albumTrackCount)
    {
        var recording =
            await SearchForRecordingAsync(album, artist, title)
                ?? throw new InvalidOperationException("Unable to match metadata with MusicBrainz Recording.");

        var release =
            recording.Releases.FirstOrDefault(x =>
                CompareHelper.ToSearchNormalisedTitle(x.Title) == CompareHelper.ToSearchNormalisedTitle(album) &&
                x.Date == releaseDate &&
                x.TrackCount == albumTrackCount &&
                x.Media.Any(y => y.Format is "Digital Media" or "CD"))
                    ?? throw new InvalidOperationException("Unable to match metadata with MusicBrainz Release.");

        var medium = release.Media.First(x => x.Format is "Digital Media" or "CD");

        var track = medium.Track.First(x => CompareHelper.ToSearchNormalisedTitle(x.Title) == CompareHelper.ToSearchNormalisedTitle(recording.Title));

        return new RecordingMatch
        {
            Recording = recording,
            Release = release,
            ReleaseGroup = release.ReleaseGroup,
            Medium = medium,
            Track = track
        };
    }

    private async Task<Recording> SearchForRecordingAsync(string album, string artist, string title)
    {
        var normalisedAlbum = CompareHelper.ToSearchNormalisedTitle(album);
        var normalisedArtist = CompareHelper.ToSearchNormalisedName(artist);
        var normalisedTitle = CompareHelper.ToSearchNormalisedTitle(title);

        Recording recording = null;

        var query = BuildQuery(album, artist, title);

        for (var offset = 0; offset < MaxPageSize * MaxPagesToSearch; offset += MaxPageSize)
        {
            // searching by artist returns no results if not in a compatible format so we won't do it for now - we'll use it for matching later
            var searchUri = $"recording/?fmt=json&limit={MaxPageSize}&offset={offset}&query={Uri.EscapeDataString(query)}";
            var response = await httpClient.GetAsync(searchUri);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<RecordingsSearchResult>(json, _jsonOptions);

            recording = FindBestMatchedRecording(searchResult, normalisedAlbum, normalisedArtist, normalisedTitle);

            if (recording != null)
            {
                return recording;
            }

            // MusicBrainz has a 1s rate limit in place
            await Task.Delay(1200);
        }

        return null;
    }

    private static string BuildQuery(string album, string artist, string title)
    {
        if (album.Any(x => x > 255) || artist.Any(x => x > 255) || title.Any(x => x > 255))
        {
            return string.Join(' ', $"{EscapeMusicBrainzQueryValue(album)} {EscapeMusicBrainzQueryValue(artist)} {EscapeMusicBrainzQueryValue(title)}".Split(' ').Distinct());
        }
        else
        {
            var searchClauses = new Dictionary<string, string>
            {
                ["recording"] = title,
                ["release"] = album
            };

            if (!artist.Contains('(') && !artist.Contains(',') && !artist.Contains(';'))
            {
                searchClauses.Add("artist", artist);
            }

            var query =
                string.Join(
                    " AND ",
                    searchClauses.Select(x =>
                        x.Value.Contains(' ')
                            ? $"({x.Key}:\"{EscapeMusicBrainzQueryValue(x.Value)}\" OR {x.Key}:\"{EscapeMusicBrainzQueryValue(x.Value.Replace(" ", ""))}\")"
                            : $"{x.Key}:\"{EscapeMusicBrainzQueryValue(x.Value)}\""));
            return query;
        }
    }

    private static string EscapeMusicBrainzQueryValue(string input)
    {
        return MusicBrainzEscapeCharactersRegex().Replace(input, @"\$1");
    }

    private static Recording FindBestMatchedRecording(RecordingsSearchResult searchResult, string album, string artist, string title)
    {
        return FindPerfectlyMatchedRecording(searchResult, album, artist, title);
    }

    private static Recording FindPerfectlyMatchedRecording(RecordingsSearchResult searchResult, string album, string artist, string title)
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

            if (recording.ArtistCredit.Any(x => !ContainsArtist(artist, x.Artist)))
            {
                continue;
            }

            return recording;
        }

        return null;
    }

    private static bool ContainsArtist(string artistText, Artist artist)
    {
        return
            ContainsName(artistText, artist.Name) ||
            artist.Aliases != null && artist.Aliases.Any(x => ContainsName(artistText, x.Name)) ||
            ContainsName(artistText, artist.SortName);
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
