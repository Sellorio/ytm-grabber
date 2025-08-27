using Sellorio.YouTubeMusicGrabber.Models.CoverArtArchive;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.CoverArtArchive
{
    internal class CoverArtArchiveService(HttpClient httpClient) : ICoverArtArchiveService
    {
        public async Task<ReleaseArtDto> GetReleaseArtAsync(Guid musicBrainzReleaseId)
        {
            var response = await httpClient.GetAsync($"release/{musicBrainzReleaseId}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ReleaseArtDto>();
        }
    }
}
