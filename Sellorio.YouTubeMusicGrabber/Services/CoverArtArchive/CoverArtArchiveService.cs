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
            return await httpClient.GetFromJsonAsync<ReleaseArtDto>($"release/{musicBrainzReleaseId}");
        }
    }
}
