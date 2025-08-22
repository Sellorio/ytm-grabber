using Sellorio.YouTubeMusicGrabber.Helpers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations
{
    internal interface IYouTubeXhrService
    {
        Task<JsonNavigator> GetNextAsync(string videoId);
        Task<JsonNavigator> GetPlayerAsync(string videoId);
    }
}