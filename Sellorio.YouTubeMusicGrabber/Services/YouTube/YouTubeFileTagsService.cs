using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Sellorio.YouTubeMusicGrabber.Models.YouTube;
using TagLib;

namespace Sellorio.YouTubeMusicGrabber.Services.YouTube;

internal class YouTubeFileTagsService(HttpClient httpClient) : IYouTubeFileTagsService
{
    public async Task UpdateFileMetadataAsync(string filename, YouTubeAlbumMetadata albumMetadata, YouTubeVideoMetadata trackMetadata)
    {
        var thumbnailBytes =
            trackMetadata.MusicMetadata.AlbumArtUrl == null
                ? null
                : await httpClient.GetByteArrayAsync(trackMetadata.MusicMetadata.AlbumArtUrl);

        using var mp3 = File.Create(filename);
        var tag = mp3.GetTag(TagTypes.Id3v2, true);
        tag.Title = trackMetadata.MusicMetadata.Title;

        if (!string.IsNullOrEmpty(trackMetadata.MusicMetadata.AlternateTitle) && trackMetadata.MusicMetadata.Title != trackMetadata.MusicMetadata.AlternateTitle)
        {
            tag.Subtitle = trackMetadata.MusicMetadata.AlternateTitle;
        }

        tag.Album = albumMetadata?.Title ?? trackMetadata.MusicMetadata.Album;
        tag.AlbumArtists = albumMetadata?.Artists;
        tag.Performers = trackMetadata.MusicMetadata.Artists.ToArray();
        tag.Year = (uint)(albumMetadata?.ReleaseYear ?? trackMetadata.MusicMetadata.ReleaseYear ?? default);
        tag.Track = (uint)trackMetadata.MusicMetadata.TrackNumber;
        tag.TrackCount = (uint)trackMetadata.MusicMetadata.TrackCount;

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
