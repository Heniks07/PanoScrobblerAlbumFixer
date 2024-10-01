namespace PanoScrobblerAlbumFixer;

public class BackupJson
{
    public List<BackupTrack>? BackupTracks { get; set; }
    public short DeletionCount { get; set; }
}

public class BackupTrack
{
    public string? Artist { get; set; }
    public long? UnixTimestamp { get; set; }
    public short? Page { get; set; }
    public string? Album { get; set; }
    public string? Track { get; set; }
    public bool Deleted { get; set; }
    public bool Scrobbled { get; set; }
}