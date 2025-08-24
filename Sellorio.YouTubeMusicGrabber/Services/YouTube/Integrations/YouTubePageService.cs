using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Services.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube.Integrations
{
    internal class YouTubePageService(HttpClient httpClient, IRateLimitService rateLimitService) : IYouTubePageService
    {
        public async Task<IList<JsonNavigator>> GetPageInitialDataAsync(string url)
        {
            const string jsonStartFragment = @"\x7d'), data: '";
            const string jsonEndFragment = @"'});";

            const string jsonStartFragment2 = ";\nytcfg.set(";
            const string jsonEndFragment2 = ");";

            List<JsonNavigator> result = null;

            await rateLimitService.WithRateLimit(RateLimits.Page, async () =>
            {
                var responseMessage = await httpClient.GetAsync(url);
                responseMessage.EnsureSuccessStatusCode();
                var responseText = await responseMessage.Content.ReadAsStringAsync();

                result = new List<JsonNavigator>(3);
                var index = 0;

                while (true)
                {
                    var jsonStartIndex = responseText.IndexOf(jsonStartFragment, index);

                    if (jsonStartIndex == -1)
                    {
                        break;
                    }

                    var jsonEndIndex = responseText.IndexOf(jsonEndFragment, jsonStartIndex + jsonStartFragment.Length);

                    if (jsonEndIndex == -1)
                    {
                        break;
                    }

                    var json =
                        DecodeXml(
                            responseText.Substring(
                                jsonStartIndex + jsonStartFragment.Length,
                                jsonEndIndex - jsonStartIndex - jsonStartFragment.Length));

                    JsonDocument document;

                    try
                    {
                        document = JsonDocument.Parse(json);
                    }
                    catch
                    {
                        throw;
                    }

                    result.Add(new(document));

                    index = jsonEndIndex;
                }

                {
                    var jsonStartIndex = responseText.IndexOf(jsonStartFragment2);

                    if (jsonStartIndex == -1)
                    {
                        return;
                    }

                    var jsonEndIndex = responseText.IndexOf(jsonEndFragment2, jsonStartIndex + jsonStartFragment2.Length);

                    if (jsonEndIndex == -1)
                    {
                        return;
                    }

                    var json =
                        responseText.Substring(
                            jsonStartIndex + jsonStartFragment2.Length,
                            jsonEndIndex - jsonStartIndex - jsonStartFragment2.Length);

                    JsonDocument document;

                    try
                    {
                        document = JsonDocument.Parse(json);
                    }
                    catch
                    {
                        throw;
                    }

                    result.Add(new(document));
                }
            });

            return result;
        }

        private static string DecodeXml(string xmlEncodedText)
        {
            return Regex.Replace(xmlEncodedText, @"(\\x22|\\x27|\\x7b|\\x7d|\\x5b|\\x5d|\\x3d|\\\\|\\)", x =>
                x.Groups[1].Value switch
                {
                    "\\x22" => "\"",
                    "\\x27" => "'",
                    "\\x7b" => "{",
                    "\\x7d" => "}",
                    "\\x5b" => "[",
                    "\\x5d" => "]",
                    "\\x3d" => "=",
                    "\\\\" => "\\",
                    "\\" => "",
                    _ => throw new InvalidOperationException(),
                });
        }
    }
}
