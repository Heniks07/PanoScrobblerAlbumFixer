using System.Diagnostics;
using PanoScrobblerAlbumFixer.API;
using Spectre.Console;

namespace PanoScrobblerAlbumFixer;

public static partial class Program
{
    private static Configuration? _config;
    private static readonly string UnscrobblerPath = Directory.GetCurrentDirectory() + "/Unscrobbler";
    private static readonly string BackupPath = Directory.GetCurrentDirectory() + "/Unscrobbler/backup/";
    private static readonly string BackupFile = $"{DateTime.Now:s}.json";

    public static void Main(string[] args)
    {
        ConfigFileCheck();
        Login();
        Debug.Assert(_config != null, nameof(_config) + " != null");


        SetupVenv();
        Console.WriteLine("Getting wrong tracks");

        var wrongTracks = GetWrongTracks();

        if (wrongTracks.Count == 0)
        {
            Console.WriteLine("No wrong tracks found, exiting");
            return;
        }

        var selectedTracks = SelectTracks(wrongTracks);

        Unscrobble(selectedTracks);

        ScrobbleAll(selectedTracks);
    }

    private static void ScrobbleAll(List<Track> selectedTracks)
    {
        do
        {
            var scrobbleCount = selectedTracks.Count > 50 ? 50 : selectedTracks.Count;

            var result = new Scrobble(_config!.ApiKey, _config.ApiSecret).ScrobbleMultipleTracks(
                selectedTracks[..scrobbleCount],
                _config.User);
            //Console.WriteLine(result);
            foreach (var selectedTrack in selectedTracks[..scrobbleCount])
            {
                var backupHandler = new BackupHandler(BackupPath, BackupFile);
                //-1 is used to indicate that the property should not be changed
                backupHandler.UpdateBackup(-1, selectedTrack, false, true);
                selectedTracks.Remove(selectedTrack);
            }
        } while (selectedTracks.Count > 0);
    }


    private static List<Track> GetWrongTracks()
    {
        var trackInfo = new TrackChecker(_config!.ApiKey, _config.User.Name);


        Console.WriteLine("How manny pages do you want to check? One Page contains 50 tracks. (Default: 1): ");
        var pages = Console.ReadLine();
        if (string.IsNullOrEmpty(pages))
            pages = "1";
        var maxPage = short.Parse(pages);

        var recentTracks = new List<Track>();

        AnsiConsole.Progress()
            .AutoClear(true)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn(),
                new ElapsedTimeColumn(),
                new RemainingTimeColumn()
            )
            .Start(ctx =>
            {
                var task1 = ctx.AddTask("[green]Getting recent tracks[/]");
                task1.MaxValue(maxPage);

                for (short i = 1; i <= maxPage; i++)
                {
                    recentTracks.AddRange(trackInfo.GetRecentTracks(i).Track);
                    task1.Increment(1);
                }
            });


        var tracks = recentTracks;

        var wrongTracks = trackInfo.GetWrongTracks(tracks.Where(x => x.Date != null).ToList());
        return wrongTracks;
    }

    private static void Unscrobble(List<Track> selectedTracks)
    {
        var backupHandler = new BackupHandler(BackupPath, BackupFile);
        backupHandler.WriteBackup(selectedTracks);

        /*AnsiConsole.Progress().AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn(),
                new ElapsedTimeColumn(),
                new RemainingTimeColumn())
            .AutoRefresh(true)
            .Start(ctx =>
            {
                var task1 = ctx.AddTask($"[green]Deleting {selectedTracks.Count} tracks[/]");
                task1.MaxValue(selectedTracks.Count);*/

        for (short i = 0; i < selectedTracks.Count; i++)
        {
            var track = selectedTracks[i];
            Console.WriteLine($"{track.Artist.Text} - {track.Name} - {track.Album.Text}");
            Debug.Assert(_config != null, nameof(_config) + " != null");
            var unscrobble = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = UnscrobblerPath,
                    FileName = UnscrobblerPath + "/venv/bin/python3",
                    //Arguments: <artist name> <Dry Run(y/n)> <Unix Timestamp> <Starting page> <username> <password> 
                    Arguments =
                        $"Unscrobbler.py \"{track.Artist.Text}\" n {track.Date?.Uts} {GetSmartPage(track.Page, i)} \"{_config.User.Name}\" \"{_config.User.Password!.Replace("\"", "\\\"")}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            unscrobble.Start();
            unscrobble.WaitForExit();

            /*task1.Increment(1);*/
            backupHandler.UpdateBackup(i, track, true, false);
        }
        /*});*/
    }

    private static short GetSmartPage(short? trackPage, short iterator)
    {
        //When a track is deleted the page number of the following tracks might be decreased by one per 50 tracks deleted 
        //It is important to use Math.Ceiling to round up, because as soon as 1,51,101,etc. tracks are deleted the page number will decrease by one for the first track on the next page
        var result = (short)(trackPage! - Math.Ceiling(iterator / 50d));

        //The page number can't be lower than 1
        return result < 1 ? (short)1 : result;
    }

    private static void SetupVenv()
    {
        if (new DirectoryInfo(UnscrobblerPath + "/venv").Exists) return;
        Console.WriteLine("venv not found, creating one and installing requirements");
        var createVenv = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = UnscrobblerPath,
                FileName = "python3",
                Arguments = "-m venv venv",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        createVenv.Start();
        createVenv.WaitForExit();

        Console.WriteLine("venv created, installing requirements");

        var installRequirements = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = UnscrobblerPath + "/venv/bin/pip3",
                Arguments = "install -r " + UnscrobblerPath + "/requirements.txt",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        installRequirements.Start();
        installRequirements.WaitForExit();

        Console.WriteLine("Requirements installed, you're ready to go!");
    }

    private static List<Track> SelectTracks(List<Track> wrongTracks)
    {
        var multiSelect = new MultiSelectionPrompt<Track>()
            .Title("Select the tracks you want to fix")
            .NotRequired()
            .Mode(SelectionMode.Independent)
            .PageSize(10)
            .InstructionsText("Use [bold]SPACE[/] to select\n" +
                              "Press [bold]ENTER[/] to confirm")
            .AddChoices(wrongTracks);

        Console.WriteLine($"Preselect all {wrongTracks.Count} tracks? (Y/n): ");
        if (Console.ReadLine()?.ToLower() != "n")
            foreach (var wrongTrack in wrongTracks)
                multiSelect.Select(wrongTrack);

        var selectedTracks = AnsiConsole.Prompt(multiSelect);
        return selectedTracks;
    }

    private static void Login()
    {
        if (_config?.User == null)
        {
            Console.WriteLine("User not found in config file, please login");
            var authentication =
                new Authentication(_config!.ApiKey, _config.ApiSecret);
            authentication.GetToken();
            Console.Write(
                "Open the URL {0} in your browser, login with your last.fm account and authorize this application\n" +
                "After you're done press Enter ",
                authentication.AuthUrl());
            Console.ReadLine();
            var user = authentication.GetSession();

            Console.WriteLine("Welcome {0}!", user.Name);

            if (_config is null)
                throw new ArgumentNullException(
                    "Config file is empty or invalid!",
                    "Delete the file and restart the application");

            _config.User = user;

            EnterPassword(user);


            WriteConfig();
        }
        else
        {
            Console.WriteLine("Welcome {0}!", _config.User.Name);
            if (string.IsNullOrEmpty(_config.User.Password))
                EnterPassword(_config.User);
        }
    }

    private static void EnterPassword(User user)
    {
        Console.Write("Now Please input your password: ");
        var pass = Console.ReadLine();
        Console.Write(
            "Password entered. Do you want to save it? It would be stored as cleartext in your config file." +
            " If you don't want to store it you have to enter it everytime you start the application (y/N): ");
        var savePass = Console.ReadLine();
        user.Password = pass;
        if (savePass?.ToLower() != "y")
        {
            Console.WriteLine("Ok, Password not saved");
            return;
        }

        if (_config != null)
        {
            Console.WriteLine("Ok, Password saved");
            _config.User.Password = pass;
        }
    }
}