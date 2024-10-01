using Spectre.Console;

namespace PanoScrobblerAlbumFixer.API;

public class TrackChecker(string apiKey, string user)
{
    public RecentTracks GetRecentTracks(short page = 1)
    {
        var url =
            $"http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={user}&api_key={apiKey}&page={page}&format=json";

        using var client = new HttpClient();
        var response = client.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
        var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        if (result.Contains("\"error\":"))
            throw new Exception("Error while fetching recent tracks\n" + result);
        var recentTracks = RecentTracksJson.FromJson(result);
        recentTracks.RecentTracks.Track.ForEach(track => track.Page = page);
        return recentTracks.RecentTracks;
    }

    public List<Track> GetWrongTracks(List<Track> tracks)
    {
        var wrongTracks = new List<Track>();

        AnsiConsole.Progress()
            .AutoClear(true)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn(),
                new ElapsedTimeColumn(), new RemainingTimeColumn())
            .Start(ctx =>
            {
                var task1 = ctx.AddTask($"[green]Checking {tracks.Count} tracks[/]");
                task1.MaxValue(tracks.Count);

                while (!ctx.IsFinished) CheckTracks(tracks, task1, wrongTracks);
            });


        return wrongTracks;
    }

    private void CheckTracks(List<Track> tracks, ProgressTask task1, List<Track> wrongTracks)
    {
        foreach (var track in tracks)
        {
            var url =
                $"http://ws.audioscrobbler.com/2.0/?method=track.getInfo&api_key={apiKey}&artist={track.Artist.Text}&track={track.Name}&format=json";
            using var client = new HttpClient();
            var response = client.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
            var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var trackinfo = TrackInfoJson.FromJson(result).Track;

            task1.Increment(1);
            if (trackinfo?.Album == null || string.IsNullOrEmpty(trackinfo?.Album.Title))
                continue;


            if (CheckGivenTrack(track))
                continue;

            //Check if the album title of the track either matches the album title or text of the scrobbled track
            if (trackinfo.Album.Title == track.Album.Title || trackinfo.Album.Title == track.Album.Text) continue;

            track.Album.Title = trackinfo.Album.Title;
            wrongTracks.Add(track);
        }
    }

    private static bool CheckGivenTrack(Track track)
    {
        //Check if the track has no album
        if (track?.Album == null)
            return true;
        //Check if the album has no title or text
        return string.IsNullOrEmpty(track?.Album.Text) && string.IsNullOrEmpty(track?.Album.Title);
    }
}