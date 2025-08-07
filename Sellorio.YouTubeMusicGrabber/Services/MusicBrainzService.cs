using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal class MusicBrainzService(HttpClient httpClient) : IMusicBrainzService
{
    private static readonly JsonSerializerOptions _jsonOptions;

    static MusicBrainzService()
    {
        _jsonOptions = new JsonSerializerOptions();
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
    }

    public async Task<RecordingMatch> FindRecordingAsync(string album, string artist, string title, DateOnly releaseDate)
    {
        // searching by artist returns no results if not in a compatible format so we won't do it for now - we'll use it for matching later
        var searchUri = $"recording/?query=recording:{WebUtility.UrlEncode(title)}+AND+release:{WebUtility.UrlEncode(album)}&fmt=json";
        var response = await httpClient.GetAsync(searchUri);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var searchResult = JsonSerializer.Deserialize<RecordingsSearchResult>(json, _jsonOptions);

        var recording =
            FindBestMatchedRecording(searchResult, artist)
                ?? throw new InvalidOperationException("Unable to match metadata with MusicBrainz Recording.");

        var release =
            recording.Releases.FirstOrDefault(x =>
                CompareHelper.ToSearchNormalisedTitle(x.Title) == CompareHelper.ToSearchNormalisedTitle(album) &&
                x.Date == releaseDate &&
                x.Media.Any(y => y.Format == "Digital Media"))
                    ?? throw new InvalidOperationException("Unable to match metadata with MusicBrainz Release.");

        return new RecordingMatch
        {
            Recording = recording,
            Release = release,
            ReleaseGroup = release.ReleaseGroup,
            Medium = release.Media.First(x => x.Format == "Digital Media")
        };
    }

    private static Recording FindBestMatchedRecording(RecordingsSearchResult searchResult, string artist)
    {
        return FindPerfectlyMatchedRecording(searchResult, artist);
    }

    private static Recording FindPerfectlyMatchedRecording(RecordingsSearchResult searchResult, string artist)
    {
        artist = CompareHelper.ToSearchNormalisedName(artist);

        foreach (var recording in searchResult.Recordings)
        {
            if (recording.Score < 100)
            {
                // assume results are in score order from 100 to lowest score
                break;
            }

            if (recording.ArtistCredit.All(x => ContainsArtist(artist, x.Artist)))
            {
                Console.WriteLine("Found a perfect match in MusicBrainz!");
                return recording;
            }
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
}
