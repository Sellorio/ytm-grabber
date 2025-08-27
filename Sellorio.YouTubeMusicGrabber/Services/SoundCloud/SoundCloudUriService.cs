using System.Text.RegularExpressions;

namespace Sellorio.YouTubeMusicGrabber.Services.SoundCloud;

internal class SoundCloudUriService : ISoundCloudUriService
{
    public bool TryParseTrackId(string uri, out string soundCloudId)
    {
        // e.g. https://soundcloud.com/nzko-kmdo/kimi-no-sei-full-version-by-sexyzone-from-a-condition-called-love-shoujo-anime
        var match = Regex.Match(uri, @"^https:\/\/soundcloud\.com\/([\w-]+\/[\w-]+)$");

        if (match.Success)
        {
            soundCloudId = match.Groups[1].Value;
            return true;
        }

        soundCloudId = null;
        return false;
    }
}
