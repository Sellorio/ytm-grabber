using System.Collections.Generic;

namespace Sellorio.YouTubeMusicGrabber.Models;

internal class ListMetadata(string id, ListMusicMetadata musicMetadata, IList<ListEntry> items)
{
    public string Id => id;
    public ListMusicMetadata MusicMetadata => musicMetadata;
    public IList<ListEntry> Items => items;
}
