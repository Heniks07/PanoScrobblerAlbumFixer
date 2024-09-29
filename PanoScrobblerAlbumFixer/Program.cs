using System.Diagnostics;
using PanoScrobblerAlbumFixer.API;
using Spectre.Console;

namespace PanoScrobblerAlbumFixer;

public static partial class Program
{
    private static Configuration? _config;
    private static readonly string UnscrobblerPath = Directory.GetCurrentDirectory() + "/Unscrobbler";

    public static void Main(string[] args)
    {
        ConfigFileCheck();
        Login();
        SetupVenv();


        var correctScrobbles = new CorrectScrobbles(_config.ApiKey, _config.User.Name);
        var recentTracks = correctScrobbles.GetRecentTracks();
        var tracks = recentTracks.Track;

        var trackInfo = new TrackChecker(_config.ApiKey);
        var wrongTracks = trackInfo.GetWrongTracks(tracks.Where(x => x.Date != null).ToList());


        var selectedTracks = SelectTracks(wrongTracks);


        foreach (var track in selectedTracks)
        {
            Console.WriteLine($"{track.Artist.Text} - {track.Name} - {track.Album.Text}");
            var unscrobble = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = UnscrobblerPath,
                    FileName = UnscrobblerPath + "/venv/bin/python3",
                    //Arguments: <artist name> <Dry Run(y/n)> <Unix Timestamp> <username> <password> 
                    Arguments =
                        $"Unscrobbler.py \"{track.Artist.Text}\" n {track.Date?.Uts} \"{_config.User.Name}\" \"{_config.User.Password.Replace("\"", "\\\"")}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            //unscrobble.Start();
            //unscrobble.WaitForExit();
        }
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

        foreach (var wrongTrack in wrongTracks) multiSelect.Select(wrongTrack);

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