using System;
using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models
{
    internal class ItemMusicMetadata(
        string title,
        string alternateTitle,
        IList<string> artists,
        string album,
        string albumId,
        int? releaseYear,
        DateOnly? releaseDate,
        int trackNumber,
        int trackCount,
        string albumArtUrl)
    {
        public string Title => title;

        public string AlternateTitle => alternateTitle;

        public IList<string> Artists => artists;

        public string Album => album;

        public string AlbumId => albumId;

        public int? ReleaseYear => releaseYear;

        public DateOnly? ReleaseDate => releaseDate;

        public int TrackNumber => trackNumber;

        public int TrackCount => trackCount;

        public string AlbumArtUrl => albumArtUrl;
    }
}
