using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Services.Common;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations
{
    internal class YouTubePageService(HttpClient httpClient, IRateLimitService rateLimitService) : IYouTubePageService
    {
        public async Task<JsonNavigator> GetPageInitialDataAsync(string url)
        {
            const string jsonStartFragment = "try {const initialData = [];initialData.push({path: '\\/guide', params: JSON.parse('\\x7b\\x7d'), data: '";
            const string jsonEndFragment = "'});ytcfg.set({'YTMUSIC_INITIAL_DATA': initialData});";

            JsonDocument jsonDocument = null;

            await rateLimitService.WithRateLimit(RateLimits.Page, async () =>
            {
                var responseMessage = await httpClient.GetAsync(url);
                responseMessage.EnsureSuccessStatusCode();
                var responseText = await responseMessage.Content.ReadAsStringAsync();
                var jsonStartIndex = responseText.IndexOf(jsonStartFragment) + jsonStartFragment.Length;
                var jsonEndIndex = responseText.IndexOf(jsonEndFragment, jsonStartIndex);
                var json = WebUtility.HtmlDecode(responseText.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex));
                jsonDocument = JsonDocument.Parse(json);
            });

            return new(jsonDocument);
        }
    }
}
