namespace Sellorio.YouTubeMusicGrabber.Services;

internal interface IYouTubeUriService
{
    string ParseVideoId(string uri);
}