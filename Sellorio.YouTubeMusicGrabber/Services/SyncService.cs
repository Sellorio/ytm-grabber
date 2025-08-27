using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Commands.Options;
using Sellorio.YouTubeMusicGrabber.Exceptions;
using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Models;
using Sellorio.YouTubeMusicGrabber.Models.Sync;
using Sellorio.YouTubeMusicGrabber.Services.Common;
using Sellorio.YouTubeMusicGrabber.Services.SoundCloud;
using Sellorio.YouTubeMusicGrabber.Services.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal class SyncService(
    IYouTubeUriService youTubeUriService,
    ISoundCloudUriService soundCloudUriService,
    IItemSourceServiceProvider itemSourceServiceProvider,
    IFileTagsService fileMetadataService,
    IYouTubeAlbumMetadataService youTubeAlbumMetadataService)
    : ISyncService
{
    private static readonly YamlDotNet.Serialization.ISerializer _yamlSerializer =
        new YamlDotNet.Serialization.SerializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .Build();

    private static readonly YamlDotNet.Serialization.IDeserializer _yamlDeserializer =
        new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .Build();

    public async Task CleanUpManifestAsync(string outputPath, IList<ManifestAlbum> manifest)
    {
        for (int albumIndex = 0; albumIndex < manifest.Count; albumIndex++)
        {
            var albumManifest = manifest[albumIndex];

            if (!Directory.Exists(Path.Combine(outputPath, albumManifest.FolderName)))
            {
                manifest.RemoveAt(albumIndex);
                albumIndex--;
                continue;
            }

            for (int trackIndex = 0; trackIndex < albumManifest.Tracks.Count; trackIndex++)
            {
                if (!File.Exists(Path.Combine(outputPath, albumManifest.FolderName, albumManifest.Tracks[trackIndex].FileName)))
                {
                    albumManifest.Tracks.RemoveAt(trackIndex);
                    trackIndex--;
                }
            }
        }

        await SaveManifestAsync(outputPath, manifest);
    }

    public async Task ProcessAddAsync(string outputPath, IList<ManifestAlbum> manifest, string uri, bool addAlbums, int? skip)
    {
        try
        {
            if (youTubeUriService.TryParseAlbumId(uri, out var albumId))
            {
                await AddAlbumAsync(outputPath, manifest, albumId, addAlbums, false, skip);
            }
            else if (youTubeUriService.TryParseTrackId(uri, out var trackId))
            {
                var itemIdResolver = itemSourceServiceProvider.GetRequiredService<IItemIdResolver>(ItemSource.YouTube);
                var resolvedTrackId = await itemIdResolver.ResolveItemIdAsync(trackId);

                var itemMetadataService = itemSourceServiceProvider.GetRequiredService<IItemMetadataService>(ItemSource.YouTube);
                var trackMetadata = await itemMetadataService.GetMetadataAsync(resolvedTrackId);

                if (addAlbums)
                {
                    await AddAlbumAsync(outputPath, manifest, trackMetadata.MusicMetadata.AlbumId, false, true, skip);
                }
                else
                {
                    var albumMetadata =
                        trackMetadata.MusicMetadata.AlbumId != null
                            ? await youTubeAlbumMetadataService.GetMetadataAsync(trackMetadata.MusicMetadata.AlbumId)
                            : null;

                    await AddTrackAsync(outputPath, manifest, albumMetadata, trackMetadata);
                }
            }
            else if (soundCloudUriService.TryParseTrackId(uri, out var soundCloudTrackId))
            {
                var itemIdResolver = itemSourceServiceProvider.GetRequiredService<IItemIdResolver>(ItemSource.SoundCloud);
                var resolvedTrackId = await itemIdResolver.ResolveItemIdAsync(soundCloudTrackId);

                var itemMetadataService = itemSourceServiceProvider.GetRequiredService<IItemMetadataService>(ItemSource.SoundCloud);
                var trackMetadata = await itemMetadataService.GetMetadataAsync(resolvedTrackId);

                await AddTrackAsync(outputPath, manifest, null, trackMetadata);
            }
            else
            {
                throw new ArgumentException("Unable to recognise album/track uri in --add.\r\n" + uri);
            }
        }
        catch (TrackUnavailableException ex)
        {
            ConsoleHelper.WriteLine(ex.ConsoleMessage, ConsoleColor.DarkRed);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteLine(ex.ToString(), ConsoleColor.Red);
        }
    }

    public async Task<IList<ManifestAlbum>> ReadOrCreateManifestAsync(string outputPath)
    {
        var manifestFilename = Path.Combine(outputPath, "ytm-manifest.yml");
        var manifest = File.Exists(manifestFilename) ? _yamlDeserializer.Deserialize<IList<ManifestAlbum>>(await File.ReadAllTextAsync(manifestFilename)) : [];
        return manifest;
    }

    private async Task AddAlbumAsync(string outputPath, IList<ManifestAlbum> manifest, string youTubeId, bool addAlbums, bool isAlbum, int? skip)
    {
        var albumMetadata = await youTubeAlbumMetadataService.GetMetadataAsync(youTubeId);

        if (isAlbum)
        {
            var albumManifest = manifest.FirstOrDefault(x => x.YouTubeId == youTubeId);

            if (albumManifest != null && albumManifest.IsFullyDownloaded)
            {
                return;
            }
        }

        foreach (var track in skip == null ? albumMetadata.Items : albumMetadata.Items.Skip(skip.Value))
        {
            if (addAlbums)
            {
                if (IsPartOfAFullyDownloadedAlbum(manifest, track.Id, out var albumId))
                {
                    ConsoleHelper.WriteLine($"Skipping already downloaded album {albumId}.", ConsoleColor.DarkGray);
                    continue;
                }
            }
            else
            {
                if (manifest.Any(x => x.Tracks.Any(x => x.YouTubeId == track.Id)))
                {
                    ConsoleHelper.WriteLine($"Skipping already downloaded track {track.Id}.", ConsoleColor.DarkGray);
                    continue;
                }
            }

            string resolvedItemId;

            try
            {
                var itemIdResolver = itemSourceServiceProvider.GetRequiredService<IItemIdResolver>(ItemSource.YouTube);
                resolvedItemId = await itemIdResolver.ResolveItemIdAsync(track.Id);
            }
            catch (TrackUnavailableException ex)
            {
                ConsoleHelper.WriteLine(ex.ConsoleMessage, ConsoleColor.DarkRed);
                continue;
            }

            if (track.Title == "[Private video]")
            {
                ConsoleHelper.WriteLine($"Skipping private upload {resolvedItemId}.", ConsoleColor.DarkYellow);
                continue;
            }

            var itemMetadataService = itemSourceServiceProvider.GetRequiredService<IItemMetadataService>(ItemSource.YouTube);

            if (addAlbums)
            {
                if (IsPartOfAFullyDownloadedAlbum(manifest, resolvedItemId, out var albumId))
                {
                    ConsoleHelper.WriteLine($"Skipping already downloaded album {albumId}.", ConsoleColor.DarkGray);
                    continue;
                }

                var metadata = await itemMetadataService.GetMetadataAsync(resolvedItemId);
                await AddAlbumAsync(outputPath, manifest, metadata.MusicMetadata.AlbumId, false, true, null);
            }
            else
            {
                if (manifest.Any(x => x.Tracks.Any(x => x.YouTubeId == resolvedItemId)))
                {
                    ConsoleHelper.WriteLine($"Skipping already downloaded track {resolvedItemId}.", ConsoleColor.DarkGray);
                    continue;
                }

                var trackMetadata = await itemMetadataService.GetMetadataAsync(resolvedItemId);
                await AddTrackAsync(outputPath, manifest, albumMetadata, trackMetadata);
            }
        }

        if (isAlbum)
        {
            var albumManifest = manifest.FirstOrDefault(x => x.YouTubeId == youTubeId);

            // album manifest will be null if the album was skipped (e.g. cannot match metadata)
            if (albumManifest != null)
            {
                albumManifest.IsFullyDownloaded = true;
                await SaveManifestAsync(outputPath, manifest);
            }
        }
    }

    private async Task AddTrackAsync(string outputPath, IList<ManifestAlbum> manifest, ListMetadata albumMetadata, ItemMetadata trackMetadata)
    {
        var outputFilename = GetTrackOutputFilename(manifest, albumMetadata, trackMetadata);
        var absoluteOutputFilename = Path.GetFullPath(Path.Combine(outputPath, outputFilename));
        var directoryPath = Path.GetDirectoryName(absoluteOutputFilename);

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var downloadService = itemSourceServiceProvider.GetRequiredService<IItemDownloadService>(trackMetadata.Source);
        await downloadService.DownloadAsMp3Async(trackMetadata, absoluteOutputFilename, (int)Quality.High);

        try
        {
            await fileMetadataService.UpdateFileMetadataAsync(absoluteOutputFilename, albumMetadata, trackMetadata);
            await AddTrackToManifestAsync(outputPath, outputFilename, manifest, albumMetadata, trackMetadata);
        }
        catch
        {
            File.Delete(absoluteOutputFilename);
            throw;
        }

        ConsoleHelper.WriteLine($"Added {trackMetadata.Id} ({trackMetadata.MusicMetadata.Title}) successfully!", ConsoleColor.Green);
    }

    private static async Task AddTrackToManifestAsync(string outputPath, string outputFilename, IList<ManifestAlbum> manifest, ListMetadata albumMetadata, ItemMetadata trackMetadata)
    {
        var folderName = Path.GetFileName(Path.GetDirectoryName(outputFilename));
        var album = manifest.FirstOrDefault(x => x.YouTubeId == albumMetadata?.Id && x.FolderName == folderName);

        if (album == null)
        {
            album = new ManifestAlbum
            {
                FolderName = folderName,
                Tracks = [],
                YouTubeId = albumMetadata?.Id
            };

            manifest.Add(album);
        }

        album.Tracks.Add(new ManifestTrack
        {
            FileName = Path.GetFileName(outputFilename),
            YouTubeId = trackMetadata.Id
        });

        await SaveManifestAsync(outputPath, manifest);
    }

    private static bool IsPartOfAFullyDownloadedAlbum(IList<ManifestAlbum> manifest, string trackYouTubeId, out string albumId)
    {
        var albumManifest = manifest.FirstOrDefault(x => x.Tracks.Any(x => x.YouTubeId == trackYouTubeId));
        albumId = albumManifest?.YouTubeId;
        return albumManifest?.IsFullyDownloaded == true;
    }

    private static async Task SaveManifestAsync(string outputPath, IList<ManifestAlbum> manifest)
    {
        var manifestFilename = Path.Combine(outputPath, "ytm-manifest.yml");
        await File.WriteAllTextAsync(manifestFilename, _yamlSerializer.Serialize(manifest));
    }

    private static string GetTrackOutputFilename(IList<ManifestAlbum> manifest, ListMetadata albumMetadata, ItemMetadata trackMetadata)
    {
        var safeAlbumName = RemoveUnsafeFilenameCharacters(albumMetadata?.MusicMetadata.Title ?? trackMetadata.MusicMetadata.Album);
        var safeTitle = RemoveUnsafeFilenameCharacters(trackMetadata.MusicMetadata.Title);

        var releaseYear = albumMetadata?.MusicMetadata.ReleaseYear ?? trackMetadata.MusicMetadata.ReleaseYear;

        var folderName = releaseYear == null ? safeAlbumName : safeAlbumName + " (" + releaseYear + ")";
        var fileName = trackMetadata.MusicMetadata.TrackNumber + " - " + safeTitle + ".mp3";

        if (albumMetadata != null &&
            manifest.Any(x => x.YouTubeId != albumMetadata.Id && x.FolderName.Equals(folderName, StringComparison.OrdinalIgnoreCase)))
        {
            folderName += " (" + albumMetadata.Id + ")";
        }

        return folderName + Path.DirectorySeparatorChar + fileName;
    }

    private static string RemoveUnsafeFilenameCharacters(string text)
    {
        var invalidCharacters = Enumerable.Union(Path.GetInvalidPathChars(), Path.GetInvalidFileNameChars());

        var result = new StringBuilder(text.Length);

        foreach (char c in text)
        {
            if (!invalidCharacters.Contains(c))
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}
