using Sellorio.YouTubeMusicGrabber.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations
{
    internal interface IYouTubePageService
    {
        Task<IList<JsonNavigator>> GetPageInitialDataAsync(string url);
    }
}