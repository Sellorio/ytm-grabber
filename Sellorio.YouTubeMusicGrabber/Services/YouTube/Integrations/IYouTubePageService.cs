using Sellorio.YouTubeMusicGrabber.Helpers;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations
{
    internal interface IYouTubePageService
    {
        Task<JsonNavigator> GetPageInitialDataAsync(string url);
    }
}