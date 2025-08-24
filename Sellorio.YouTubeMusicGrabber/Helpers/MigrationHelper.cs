using Sellorio.YouTubeMusicGrabber.Models.Sync;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Helpers
{
    internal static class MigrationHelper
    {
        private static readonly YamlDotNet.Serialization.IDeserializer _yamlDeserializer =
            new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .Build();

        private const string _oldOutputPath = "./OldMusic";
        private static Dictionary<string, string> _youTubeIdToPathMapping;

        public static async Task<bool> TryImportFromOldMusicAsync(YouTubeTrackMetadata track, string outputFilename)
        {
            if (_youTubeIdToPathMapping == null)
            {
                var oldManifest = await ReadOrCreateManifestAsync(_oldOutputPath);
                CleanUpManifest(_oldOutputPath, oldManifest);
                _youTubeIdToPathMapping =
                    oldManifest
                        .Select(x => x.Tracks.Select(y => new KeyValuePair<string, string>(y.YouTubeId, Path.Combine(_oldOutputPath, x.FolderName, y.FileName))))
                        .SelectMany(x => x)
                        .ToDictionary(x => x.Key, x => x.Value);
            }

            if (File.Exists(outputFilename))
            {
                return true;
            }
            
            if (_youTubeIdToPathMapping.TryGetValue(track.Id, out var existingFilePath) && File.Exists(existingFilePath))
            {
                File.Move(existingFilePath, outputFilename);

                try
                {
                    using var mp3 = TagLib.File.Create(outputFilename);
                    var tag = mp3.GetTag(TagLib.TagTypes.Id3v2, true);

                    tag.Subtitle = null;
                    tag.MusicBrainzReleaseGroupId = null;
                    tag.MusicBrainzReleaseId = null;
                    tag.MusicBrainzDiscId = null;
                    tag.MusicBrainzTrackId = null;

                    mp3.Save();
                }
                catch
                {
                    File.Move(outputFilename, existingFilePath);
                    throw;
                }

                ConsoleHelper.WriteLine("File imported from old music.", System.ConsoleColor.Cyan);

                return true;
            }

            return false;
        }

        private static void CleanUpManifest(string outputPath, IList<OldManifestAlbum> manifest)
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
        }

        private static async Task<IList<OldManifestAlbum>> ReadOrCreateManifestAsync(string outputPath)
        {
            var manifestFilename = Path.Combine(outputPath, "ytm-manifest.yml");
            var manifest = File.Exists(manifestFilename) ? _yamlDeserializer.Deserialize<IList<OldManifestAlbum>>(await File.ReadAllTextAsync(manifestFilename)) : [];
            return manifest;
        }
    }
}
