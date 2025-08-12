using System.Collections.Generic;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.Sync;

namespace Sellorio.YouTubeMusicGrabber.Services;
internal interface ISyncService
{
    Task CleanUpManifestAsync(string outputPath, IList<ManifestAlbum> manifest);
    Task ProcessAddAsync(string outputPath, IList<ManifestAlbum> manifest, string uri, bool addAlbums);
    Task<IList<ManifestAlbum>> ReadOrCreateManifestAsync(string outputPath);
}