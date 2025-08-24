using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;
using TagLib;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube;

internal class YouTubeFileTagsService(HttpClient httpClient) : IYouTubeFileTagsService
{
    public async Task UpdateFileMetadataAsync(string filename, YouTubeAlbumMetadata albumMetadata, YouTubeTrackMetadata trackMetadata, int trackNumber)
    {
        var bestThumbnailPreferenceScore =
            trackMetadata.Thumbnails != null && trackMetadata.Thumbnails.Any()
                ? trackMetadata.Thumbnails.Min(x => x.Preference)
                : 0;

        var preferredThumbnail =
            trackMetadata.Thumbnails
                .Where(x => x.Preference == bestThumbnailPreferenceScore && x.Width == x.Height && x.Width < 500)
                .OrderByDescending(x => x.Width)
                .FirstOrDefault();

        var thumbnailBytes = preferredThumbnail == null ? null : await httpClient.GetByteArrayAsync(preferredThumbnail.Url);

        using var mp3 = File.Create(filename);
        var tag = mp3.GetTag(TagTypes.Id3v2, true);
        tag.Title = trackMetadata.Title;

        if (!string.IsNullOrEmpty(trackMetadata.AlternateTitle) && trackMetadata.Title != trackMetadata.AlternateTitle)
        {
            tag.Subtitle = trackMetadata.AlternateTitle;
        }

        tag.Album = albumMetadata.Title;
        tag.AlbumArtists = albumMetadata.Artists;
        tag.Performers = trackMetadata.Artists.ToArray();
        tag.Year = (uint)(albumMetadata.ReleaseYear ?? default);
        tag.Track = (uint)trackNumber;
        tag.TrackCount = (uint)albumMetadata.Tracks.Count;

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
