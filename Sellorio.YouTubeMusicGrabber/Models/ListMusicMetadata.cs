using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models;

internal class ListMusicMetadata(string title, IList<string> artists, int? releaseYear)
{
    public string Title => title;
    public IList<string> Artists => artists;
    public int? ReleaseYear => releaseYear;
}
