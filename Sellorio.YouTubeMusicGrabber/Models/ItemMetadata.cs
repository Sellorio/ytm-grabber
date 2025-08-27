namespace Sellorio.YouTubeMusicGrabber.Models;

internal class ItemMetadata(string id, ItemSource source, ItemMusicMetadata musicMetadata, string filenameHint)
{
    public string Id => id;
    public ItemSource Source => source;
    public ItemMusicMetadata MusicMetadata => musicMetadata;
    public string FilenameHint => filenameHint;
}
