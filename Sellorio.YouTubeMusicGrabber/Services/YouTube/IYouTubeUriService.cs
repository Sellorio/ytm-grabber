namespace Sellorio.YouTubeMusicGrabber.Services.YouTube;

internal interface IYouTubeUriService
{
    bool TryParseAlbumId(string uri, out string id);
    bool TryParseTrackId(string uri, out string id);
}