using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Commands.Options;
using Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;
using Sellorio.YouTubeMusicGrabber.Models.Sync;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal class SyncService(
    IYouTubeUriService youTubeUriService,
    IYouTubeApiService youTubeApiService,
    IMusicBrainzService musicBrainzService,
    IYouTubeDownloadService youTubeDownloadService,
    IFileMetadataService fileMetadataService,
    IYouTubeMetadataService youTubeMetadataService)
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

    public async Task ProcessAddAsync(string outputPath, IList<ManifestAlbum> manifest, string uri, bool addAlbums)
    {
        try
        {
            if (youTubeUriService.TryParseAlbumId(uri, out var albumId))
            {
                await AddAlbumAsync(outputPath, manifest, albumId, addAlbums, false);
            }
            else if (youTubeUriService.TryParseTrackId(uri, out var trackId))
            {
                var additionalTrackInfo = await youTubeMetadataService.GetTrackAdditionalInfoAsync(trackId);
                var albumTracksList = await youTubeApiService.GetPlaylistEntriesAsync(additionalTrackInfo.AlbumId);
                await AddTrackAsync(outputPath, manifest, trackId, additionalTrackInfo, albumTracksList.Count);
            }
            else
            {
                throw new ArgumentException("Unable to recognise album/track uri in --add.\r\n" + uri);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public async Task<IList<ManifestAlbum>> ReadOrCreateManifestAsync(string outputPath)
    {
        var manifestFilename = Path.Combine(outputPath, "ytm-manifest.yml");
        var manifest = File.Exists(manifestFilename) ? _yamlDeserializer.Deserialize<IList<ManifestAlbum>>(await File.ReadAllTextAsync(manifestFilename)) : [];
        return manifest;
    }

    private async Task AddAlbumAsync(string outputPath, IList<ManifestAlbum> manifest, string youTubeId, bool addAlbums, bool isAlbum)
    {
        var tracksList = await youTubeApiService.GetPlaylistEntriesAsync(youTubeId);

        foreach (var track in tracksList)
        {
            if (track.Title == "[Private video]")
            {
                Console.WriteLine($"Skipping private upload {track.Id}.");
                continue;
            }

            if (addAlbums)
            {
                var additionalTrackInfo = await youTubeMetadataService.GetTrackAdditionalInfoAsync(track.Id);
                await AddAlbumAsync(outputPath, manifest, additionalTrackInfo.AlbumId, false, true);
            }
            else
            {
                var trackCount = tracksList.Count;
                var additionalTrackInfo = await youTubeMetadataService.GetTrackAdditionalInfoAsync(track.Id);

                if (!isAlbum)
                {
                    var albumTracksList = await youTubeApiService.GetPlaylistEntriesAsync(additionalTrackInfo.AlbumId);
                    trackCount = albumTracksList.Count;
                }

                await AddTrackAsync(outputPath, manifest, track.Id, additionalTrackInfo, trackCount);
            }
        }
    }

    private async Task AddTrackAsync(string outputPath, IList<ManifestAlbum> manifest, string youTubeId, YouTubeTrackAdditionalInfo additionalTrackInfo, int albumTrackCount)
    {
        if (manifest.Any(x => x.Tracks.Any(x => x.YouTubeId == youTubeId)))
        {
            Console.WriteLine($"Skipping already downloaded {youTubeId}.");
            return;
        }

        var metadata = await youTubeApiService.GetTrackMetadataAsync(youTubeId);
        
        await AddTrackAsync(outputPath, manifest, metadata, additionalTrackInfo, albumTrackCount);
    }

    private async Task AddTrackAsync(string outputPath, IList<ManifestAlbum> manifest, YouTubeTrackMetadata metadata, YouTubeTrackAdditionalInfo additionalTrackInfo, int albumTrackCount)
    {
        var musicBrainzMetadata = await musicBrainzService.FindRecordingAsync(metadata.Album, metadata.Artists[0], metadata.Title, metadata.ReleaseDate, albumTrackCount);
        var outputFilename = GetTrackOutputFilename(metadata, musicBrainzMetadata.Track);
        var absoluteOutputFilename = Path.GetFullPath(Path.Combine(outputPath, outputFilename));
        var directoryPath = Path.GetDirectoryName(absoluteOutputFilename);

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await youTubeDownloadService.DownloadAsMp3Async(metadata, absoluteOutputFilename, (int)Quality.High);

        try
        {
            await fileMetadataService.UpdateFileMetadataAsync(absoluteOutputFilename, metadata, musicBrainzMetadata);
            await AddTrackToManifestAsync(outputPath, outputFilename, manifest, metadata, musicBrainzMetadata, additionalTrackInfo);
        }
        catch
        {
            File.Delete(absoluteOutputFilename);
            throw;
        }

        Console.WriteLine($"Added {metadata.Id} ({musicBrainzMetadata.Recording.Title}) successfully!");
    }

    private async Task AddTrackToManifestAsync(string outputPath, string outputFilename, IList<ManifestAlbum> manifest, YouTubeTrackMetadata metadata, RecordingMatch musicBrainzMetadata, YouTubeTrackAdditionalInfo additionalTrackInfo)
    {
        var album = manifest.FirstOrDefault(x => x.MusicBrainzId == musicBrainzMetadata.Release.Id);

        if (album == null)
        {
            album = new ManifestAlbum
            {
                FolderName = Path.GetFileName(Path.GetDirectoryName(outputFilename)),
                MusicBrainzId = musicBrainzMetadata.Release.Id,
                Tracks = [],
                YouTubeId = additionalTrackInfo.AlbumId
            };

            manifest.Add(album);
        }

        album.Tracks.Add(new ManifestTrack
        {
            FileName = Path.GetFileName(outputFilename),
            MusicBrainzId = musicBrainzMetadata.Track.Id,
            YouTubeId = metadata.Id
        });

        await SaveManifestAsync(outputPath, manifest);
    }

    private static async Task SaveManifestAsync(string outputPath, IList<ManifestAlbum> manifest)
    {
        var manifestFilename = Path.Combine(outputPath, "ytm-manifest.yml");
        await File.WriteAllTextAsync(manifestFilename, _yamlSerializer.Serialize(manifest));
    }

    private static string GetTrackOutputFilename(YouTubeTrackMetadata metadata, Track track)
    {
        var safeAlbumName = RemoveUnsafeFilenameCharacters(metadata.Album);
        var safeTitle = RemoveUnsafeFilenameCharacters(metadata.Title);

        return
            Path.Combine(
                safeAlbumName + " (" + metadata.ReleaseYear + ")",
                track.Number + " - " + safeTitle + ".mp3");
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
