using System.Diagnostics;
using Newtonsoft.Json;
using PanoScrobblerAlbumFixer.API;

namespace PanoScrobblerAlbumFixer;

public class BackupHandler(string backupPath, string BackupFile)
{
    public void WriteBackup(List<Track> tracks)
    {
        BackupJson backupJson = new();
        var backupTrack = tracks.Select(track => new BackupTrack
            {
                Artist = track.Artist.Text,
                UnixTimestamp = track.Date.Uts,
                Page = track.Page,
                Album = track.Album.Title,
                Track = track.Name,
                Deleted = false,
                Scrobbled = false
            })
            .ToList();
        backupJson.BackupTracks = backupTrack;
        backupJson.DeletionCount = 0;
        var jsonString = JsonConvert.SerializeObject(backupJson, Formatting.Indented);

        Directory.CreateDirectory(backupPath);

        File.WriteAllText(backupPath + BackupFile, jsonString);
        Console.WriteLine($"Backup written to {backupPath}{BackupFile}");
    }

    private void WriteBackup(BackupJson backupJson)
    {
        var jsonString = JsonConvert.SerializeObject(backupJson, Formatting.Indented);

        Directory.CreateDirectory(backupPath);
        File.WriteAllText(backupPath + BackupFile, jsonString);
    }

    public List<Track> ReadBackup()
    {
        var backupJson = ReadBackupTracks();
        var tracks = backupJson.BackupTracks.Select(backupTrack => new Track
            {
                Artist = new Album { Text = backupTrack.Artist },
                Date = new Date { Uts = backupTrack.UnixTimestamp },
                Page = backupTrack.Page,
                Album = new Album { Title = backupTrack.Album },
                Name = backupTrack.Track
            })
            .ToList();
        return tracks;
    }

    private BackupJson? ReadBackupTracks()
    {
        var jsonString = File.ReadAllText(backupPath + BackupFile);
        var backupJson = JsonConvert.DeserializeObject<BackupJson>(jsonString);
        return backupJson;
    }

    public void UpdateBackup(short deletedIndex, Track track, bool deleted, bool scrobbled)
    {
        var backupJson = ReadBackupTracks();

        Debug.Assert(backupJson?.BackupTracks != null, "backupJson?.BackupTracks != null");
        var updatedTrack =
            backupJson?.BackupTracks.FirstOrDefault(x =>
                x.UnixTimestamp == track.Date.Uts && x.Artist == track.Artist.Text);

        if (updatedTrack == null)
            return;

        backupJson?.BackupTracks.Remove(updatedTrack);
        updatedTrack.Deleted = deleted || updatedTrack.Deleted;
        updatedTrack.Scrobbled = scrobbled || updatedTrack.Scrobbled;

        if (!updatedTrack.Deleted || !updatedTrack.Scrobbled)
            backupJson?.BackupTracks.Add(updatedTrack);
        if (deletedIndex > 0)
            backupJson!.DeletionCount = deletedIndex;

        if (backupJson != null)
            WriteBackup(backupJson);
    }
}