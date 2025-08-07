using System;
using System.Text.RegularExpressions;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal partial class YouTubeUriService : IYouTubeUriService
{
    public string ParseVideoId(string uri)
    {
        var match = YouTubeUriRegex().Match(uri);

        if (!match.Success)
        {
            throw new ArgumentException("Unexpected youtube uri.");
        }

        var youtubeId = match.Groups[1].Value;

        return youtubeId;
    }

    // Supported Uri Formats:
    // https://music.youtube.com/watch?v=Cqp-dB7GVI8&list=PLIr8oAMYGij0QrgUfzLyqbwrHfaBtXL1w
    // https://youtu.be/Cqp-dB7GVI8
    // https://www.youtube.com/watch?v=Cqp-dB7GVI8
    [GeneratedRegex(@"^https:\/\/(?:music\.youtube\.com\/watch\?v=|youtu\.be\/|www\.youtube\.com\/watch\?v=)([a-zA-Z0-9-]+)[&a-zA-Z0-9=_]*$", RegexOptions.IgnoreCase)]
    private static partial Regex YouTubeUriRegex();
}
