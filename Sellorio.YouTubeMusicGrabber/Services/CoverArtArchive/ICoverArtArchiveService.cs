using Sellorio.YouTubeMusicGrabber.Models.CoverArtArchive;
using System;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.CoverArtArchive
{
    internal interface ICoverArtArchiveService
    {
        Task<ReleaseArtDto> GetReleaseArtAsync(Guid musicBrainzReleaseId);
    }
}