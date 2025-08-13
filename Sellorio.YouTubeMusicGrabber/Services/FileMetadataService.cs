using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Helpers;
using Sellorio.YouTubeMusicGrabber.Models.MusicBrainz;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;
using TagLib;

namespace Sellorio.YouTubeMusicGrabber.Services;

internal class FileMetadataService(HttpClient httpClient) : IFileMetadataService
{
    public async Task UpdateFileMetadataAsync(string filename, YouTubeTrackMetadata youTubeMetadata, RecordingMatch musicBrainzMetadata)
    {
        var bestThumbnailPreferenceScore =
            youTubeMetadata.Thumbnails != null && youTubeMetadata.Thumbnails.Any()
                ? youTubeMetadata.Thumbnails.Min(x => x.Preference)
                : 0;

        var preferredThumbnail =
            youTubeMetadata.Thumbnails
                .Where(x => x.Preference == bestThumbnailPreferenceScore && x.Width == x.Height && x.Width < 500)
                .OrderByDescending(x => x.Width)
                .FirstOrDefault();

        var thumbnailBytes = preferredThumbnail == null ? null : await httpClient.GetByteArrayAsync(preferredThumbnail.Url);

        using var mp3 = File.Create(filename);
        var tag = mp3.GetTag(TagTypes.Id3v2, true);
        tag.Title = musicBrainzMetadata.Track.Title;

        if (CompareHelper.ToSearchNormalisedTitle(musicBrainzMetadata.Track.Title) != CompareHelper.ToSearchNormalisedTitle(youTubeMetadata.Title))
        {
            // for tracks that have an english translation in YouTube, add that in as a subtitle
            tag.Subtitle = youTubeMetadata.Title;
        }

        tag.Album = musicBrainzMetadata.Release.Title;
        tag.AlbumArtists = musicBrainzMetadata.Release.ArtistCredit.Select(x => x.Name).ToArray();
        tag.Performers = musicBrainzMetadata.Recording.ArtistCredit.Select(x => x.Name).ToArray();
        tag.Year = (uint)musicBrainzMetadata.Release.ReleaseYear;
        _ = uint.TryParse(musicBrainzMetadata.Track.Number, out var trackNumber);
        tag.Track = trackNumber;
        tag.TrackCount = (uint)musicBrainzMetadata.Release.TrackCount;

        tag.MusicBrainzReleaseGroupId = musicBrainzMetadata.ReleaseGroup.Id.ToString();
        tag.MusicBrainzReleaseId = musicBrainzMetadata.Release.Id.ToString();
        tag.MusicBrainzDiscId = musicBrainzMetadata.Medium.Id.ToString();
        tag.MusicBrainzTrackId = musicBrainzMetadata.Track.Id.ToString();

        if (thumbnailBytes != null)
        {
            var albumArtPicture = new Picture
            {
                Type = PictureType.FrontCover,
                MimeType = "image/jpeg",
                Data = new ByteVector(thumbnailBytes)
            };

            tag.Pictures = [];
            tag.Pictures = [albumArtPicture];
        }

        mp3.Save();
    }
}
