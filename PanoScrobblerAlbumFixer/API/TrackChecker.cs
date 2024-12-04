using System.Text.RegularExpressions;
using Spectre.Console;

namespace PanoScrobblerAlbumFixer.API;

public class TrackChecker(string apiKey, string user)
{
    public RecentTracks GetRecentTracks(short page)
    {
        var url =
            $"https://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={user}&api_key={apiKey}&page={page}&format=json";

        using var client = new HttpClient();
        var response = client.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
        var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        if (result.Contains("\"error\":"))
        {
            throw new HttpRequestException("Error while fetching recent tracks\n" + result);
        }

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
                var task1 = ctx.AddTask($"[green]Checking {tracks.Count} tracks (found 00 tracks)[/]");
                task1.MaxValue(tracks.Count);

                while (!ctx.IsFinished)
                {
                    CheckTracks(tracks, task1, wrongTracks);
                }
            });


        return wrongTracks;
    }

    private void CheckTracks(List<Track> tracks, ProgressTask task1, List<Track> wrongTracks)
    {
        foreach (var track in tracks)
        {
            try
            {
                var url =
                    $"https://ws.audioscrobbler.com/2.0/?method=track.getInfo&api_key={apiKey}&artist={track.Artist.Text}&track={track.Name}&format=json";
                using var client = new HttpClient();
                var response = client.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
                var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                var trackInfo = TrackInfoJson.FromJson(result).Track;

                task1.Increment(1);

                if (NameChecks(trackInfo, track))
                {
                    continue;
                }

                if (AdvancedChecks(trackInfo, track))
                {
                    continue;
                }

                track.OldAlbum = track.Album.Text;

                track.Album.Title = string.IsNullOrEmpty(trackInfo.Album?.Title) ? "" : trackInfo.Album.Title;
                track.Album.Text = string.IsNullOrEmpty(trackInfo.Album?.Title) ? "" : trackInfo.Album.Title;

                wrongTracks.Add(track);
                task1.Description = $"[green]Checking {tracks.Count} tracks (found {wrongTracks.Count:D2} tracks)[/]";
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]{e.Message}[/]");
            }
        }

        task1.StopTask();
    }

    /// <summary>
    ///     This method is used to check if the track is correct by checking how manny listeners / scrobbles the album has in
    ///     comparison to the album the request returned
    /// </summary>
    /// <param name="trackInfo">The correct track from the request to lastfm</param>
    /// <param name="track">The track that was scrobbled</param>
    /// <returns>Returns true if the track should be skipped</returns>
    private bool AdvancedChecks(InfoTrack trackInfo, Track track)
    {
        var albumChecker = new AlbumChecker(apiKey);
        var scrobblePlaycount = albumChecker.GetPlaycount(track.Album.Text, track.Artist.Text);
        if (trackInfo.Album == null || string.IsNullOrEmpty(trackInfo.Album.Title))
        {
            return scrobblePlaycount / (double)trackInfo.Playcount! > 0.1;
        }

        var trackPlaycount = albumChecker.GetPlaycount(trackInfo.Album.Title, track.Artist.Text);
        return scrobblePlaycount / (double)trackPlaycount > 0.5;
    }

    private static bool NameChecks(InfoTrack trackInfo, Track track)
    {
        //Check if the track isn't supposed to have an album
        if (string.IsNullOrEmpty(trackInfo.Album?.Title) &&
            string.IsNullOrEmpty(track.Album?.Text) &&
            string.IsNullOrEmpty(track.Album?.Title))
        {
            return true;
        }

        if (string.IsNullOrEmpty(trackInfo.Album?.Title))
        {
            return false;
        }

        //Check if the track has no album
        if (CheckGivenTrack(track))
        {
            return true;
        }

        //Check if the album title of the track matches the album text of the scrobbled track
        //Ignores all whitespaces
        var albumTextMatch = Regex.Match(track.Album.Text.Replace(" ", ""),
            $"{CleanUpForRegexPattern(trackInfo.Album.Title)}.*".Replace(" ", ""), RegexOptions.IgnoreCase);


        return albumTextMatch.Success;
    }

    private static string CleanUpForRegexPattern(string input)
    {
        return input.Replace("(", "\\(").Replace(")", "\\)").Replace("[", "\\[").Replace("]", "\\]").Replace("{", "\\{")
            .Replace("}", "\\}") //Escape brackets
            .Replace(":", "[-:]"); //colons and hyphens are interchangeable
    }

    private static bool CheckGivenTrack(Track track)
    {
        //Check if the track has no album
        if (track.Album == null)
        {
            return true;
        }

        //Check if the album has no title or text
        return string.IsNullOrEmpty(track.Album.Text) && string.IsNullOrEmpty(track.Album.Title);
    }
}