namespace Sellorio.YouTubeMusicGrabber.Services.SoundCloud;

internal interface ISoundCloudUriService
{
    bool TryParseTrackId(string uri, out string soundCloudId);
}