using Sellorio.YouTubeMusicGrabber.Services.YouTube;
using System.Text.RegularExpressions;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal partial class YouTubeUriService : IYouTubeUriService
{
    public bool TryParseTrackId(string uri, out string id)
    {
        var match = YouTubeTrackUriRegex().Match(uri);

        if (!match.Success)
        {
            id = null;
            return false;
        }

        id = match.Groups[1].Value;

        return true;
    }

    public bool TryParseAlbumId(string uri, out string id)
    {
        var match = YouTubeAlbumUriRegex().Match(uri);

        if (!match.Success)
        {
            id = null;
            return false;
        }

        id = match.Groups[1].Value;

        return true;
    }

    // Supported Uri Formats:
    // https://music.youtube.com/watch?v=Cqp-dB7GVI8&list=PLIr8oAMYGij0QrgUfzLyqbwrHfaBtXL1w
    // https://youtu.be/Cqp-dB7GVI8
    // https://www.youtube.com/watch?v=Cqp-dB7GVI8
    [GeneratedRegex(@"^https:\/\/(?:music\.youtube\.com\/watch\?v=|youtu\.be\/|www\.youtube\.com\/watch\?v=)([a-zA-Z0-9_-]+)[&a-zA-Z0-9=_]*$", RegexOptions.IgnoreCase)]
    private static partial Regex YouTubeTrackUriRegex();

    // Supported Uri Formats:
    // https://music.youtube.com/playlist?list=Cqp-dB7GVI8
    // https://www.youtube.com/playlist?list=Cqp-dB7GVI8
    [GeneratedRegex(@"^https:\/\/(?:music\.youtube\.com\/playlist\?list=|www\.youtube\.com\/playlist\?list=)([a-zA-Z0-9_-]+)[&a-zA-Z0-9=_]*$", RegexOptions.IgnoreCase)]
    private static partial Regex YouTubeAlbumUriRegex();
}
