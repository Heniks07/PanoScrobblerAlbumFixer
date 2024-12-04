using System.Diagnostics;
using PanoScrobblerAlbumFixer.API;
using Spectre.Console;

namespace PanoScrobblerAlbumFixer;

public static partial class Program
{
    private static Configuration? _config;

    public static void Main()
    {
        ConfigFileCheck();
        Login();

        Debug.Assert(_config != null, nameof(_config) + " != null");

        AnsiConsole.MarkupLine("[dim]Getting wrong tracks[/]");

        var wrongTracks = GetWrongTracks();

        if (wrongTracks.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No wrong tracks found![/]");
            return;
        }

        var selectedTracks = SelectTracks(wrongTracks);

        if (selectedTracks.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No tracks selected![/]");
            return;
        }


        AnsiConsole.MarkupLine("[dim]Unscrobbling selected tracks[/]");
        Unscrobble(selectedTracks);

        AnsiConsole.MarkupLine("[dim]Scrobbling selected tracks[/]");

        ScrobbleAll(selectedTracks);
    }

    private static void ScrobbleAll(List<Track> selectedTracks)
    {
        var totalTracks = selectedTracks.Count;
        var scrobbledTracks = 0;
        do
        {
            var scrobbleCount = selectedTracks.Count > 50 ? 50 : selectedTracks.Count;
            scrobbledTracks += scrobbleCount;

            Debug.Assert(_config != null, nameof(_config) + " != null");
            new Scrobble(_config).ScrobbleMultipleTracks(
                selectedTracks[..scrobbleCount],
                _config.User);

            foreach (var selectedTrack in selectedTracks[..scrobbleCount])
            {
                //-1 is used to indicate that the property should not be changed
                selectedTracks.Remove(selectedTrack);
            }

            AnsiConsole.MarkupLine("[green]Scrobbled {0}/{1} tracks[/]", scrobbledTracks, totalTracks);
        } while (selectedTracks.Count > 0);
    }


    private static List<Track> GetWrongTracks()
    {
        var trackInfo = new TrackChecker(_config!.ApiKey, _config.User.Name);


        var minPage = AnsiConsole.Prompt(new TextPrompt<short>("From which page do you want to start checking?")
            .DefaultValue((short)1)
            .ShowDefaultValue(true)
        );
        if (minPage < 1)
        {
            minPage = 1;
        }

        var internalMinPage = minPage;


        var maxPage = AnsiConsole.Prompt(new TextPrompt<short>("At which page do you want to stop checking?")
            .DefaultValue(internalMinPage)
            .ShowDefaultValue(true)
            .Validate(n => n >= internalMinPage)
            .ValidationErrorMessage("[red]This number must be greater or equal than the starting page[/]")
        );


        var recentTracks = new List<Track>();
        var internalMaxPage = maxPage;

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
                task1.MaxValue(internalMaxPage);

                for (var i = internalMinPage; i <= internalMaxPage; i++)
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
        CheckOlderThan2Weeks(selectedTracks);
        if (selectedTracks.Count == 0) return;

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
                var task1 = ctx.AddTask("[green]Unscrobbling tracks[/]");
                task1.MaxValue(selectedTracks.Count);


                if (_config?.User == null) return;

                var unscrobble = new Unscrobble(_config);

                foreach (var track in selectedTracks)
                {
                    unscrobble.UnscrobbleTrack(track);
                    task1.Increment(1);
                }
            });
    }

    private static void CheckOlderThan2Weeks(List<Track> selectedTracks)
    {
        if (!selectedTracks.Exists(x => x.Date.Uts < DateTimeOffset.Now.AddDays(-14).ToUnixTimeSeconds())) return;

        AnsiConsole.MarkupLine(
            "[red]Some tracks are older than 2 weeks and can't be scrobbled at the same time![/]\n " +
            "It is possible to scrobble them to a earlier date, but this might mess up your scrobble history!");
        var scrobbleOlder =
            AnsiConsole.Prompt(new ConfirmationPrompt("Do you want to scrobble them to an earlier date?").ShowChoices()
                .ShowDefaultValue());
        if (scrobbleOlder)
        {
            return;
        }

        selectedTracks.RemoveAll(x => x.Date.Uts < DateTimeOffset.Now.AddDays(-14).ToUnixTimeSeconds());
        AnsiConsole.MarkupLine(
            "[green]Older tracks have been removed![/] Will only delete tracks from the last 2 weeks");
    }


    private static List<Track> SelectTracks(List<Track> wrongTracks)
    {
        wrongTracks.ForEach(x => x.Album.Text = x.Album.Text.Replace("[", "(").Replace("]", ")"));
        wrongTracks.ForEach(x => x.Name = x.Name.Replace("[", "(").Replace("]", ")"));
        wrongTracks.ForEach(x => x.Artist.Text = x.Artist.Text.Replace("[", "(").Replace("]", ")"));
        wrongTracks.ForEach(x => x.OldAlbum = x.OldAlbum.Replace("[", "(").Replace("]", ")"));


        var multiSelect = new MultiSelectionPrompt<Track>()
            .Title("Select the tracks you want to fix")
            .NotRequired()
            .Mode(SelectionMode.Independent)
            .PageSize(10)
            .InstructionsText("Use [bold]SPACE[/] to select\n" +
                              "Press [bold]ENTER[/] to confirm")
            .AddChoices(wrongTracks);

        var preselect = AnsiConsole.Prompt(
            new ConfirmationPrompt(
                    $"Do you want to mark all {wrongTracks.Count} tracks as selected? (You can deselect them later)")
                .ShowChoices(true)
                .ShowDefaultValue(true)
        );
        if (preselect)
        {
            foreach (var wrongTrack in wrongTracks)
            {
                multiSelect.Select(wrongTrack);
            }
        }

        var selectedTracks = AnsiConsole.Prompt(multiSelect);
        return selectedTracks;
    }

    private static void Login()
    {
        if (_config?.User == null)
        {
            AnsiConsole.MarkupLine("[bold red]User not found![/] Please login to your Last.fm account");
            var authentication =
                new Authentication(_config!.ApiKey, _config.ApiSecret);
            authentication.GetToken();
            AnsiConsole.MarkupLine(
                "Open the URL [blue]{0}[/] in your browser, login with your last.fm account and authorize this application Press [bold] Enter [/] when you're done",
                authentication.AuthUrl());
            Console.ReadLine();
            var user = authentication.GetSession();

            AnsiConsole.MarkupLine("[green]Welcome [bold]{0}[/]![/]", user.Name);

            _config.User = user;

            _config.User.Password = EnterPassword(out var savePassword);


            WriteConfig(savePassword);
        }
        else
        {
            AnsiConsole.MarkupLine("[green]Welcome [bold]{0}[/]![/]", _config.User.Name);
            if (string.IsNullOrEmpty(_config.User.Password))
            {
                _config.User.Password = EnterPassword(out _);
            }
        }

        if (!string.IsNullOrEmpty(_config.User.CsrfToken) && !string.IsNullOrEmpty(_config.User.SessionId)) return;

        var saveCookie = GetCookieValues();

        WriteConfig(saveCookie: saveCookie);
    }

    private static User LoginGetUser(string apiKey, string apiSecret, out bool savePassword)
    {
        AnsiConsole.MarkupLine("[bold red]User not found![/] Please login to your Last.fm account");
        var authentication =
            new Authentication(apiKey, apiSecret);
        authentication.GetToken();
        AnsiConsole.MarkupLine(
            "Open the URL [blue]{0}[/] in your browser, login with your last.fm account and authorize this application Press [bold] Enter [/] when you're done",
            authentication.AuthUrl());
        Console.ReadLine();
        var user = authentication.GetSession();

        AnsiConsole.MarkupLine("[green]Welcome [bold]{0}[/]![/]", user.Name);

        user.Password = EnterPassword(out savePassword);


        return user;
    }

    private static bool GetCookieValues()
    {
        AnsiConsole.MarkupLine("[red]No CSRF token or Session ID found![/] Please login to your Last.fm account");
        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How do you want to login?")
                .AddChoices("Using Firefox", "Manually typing the CSRF token and Session ID"));

        if (selection == "Manually typing the CSRF token and Session ID")
        {
            if (_config != null)
            {
                _config.User.CsrfToken = AnsiConsole.Prompt(new TextPrompt<string>("Please enter your CSRF token:"));
                _config.User.SessionId = AnsiConsole.Prompt(new TextPrompt<string>("Please enter your Session ID:"));
            }
        }
        else if (_config != null)
        {
            new CookieRetriever(_config).Login();
        }


        var saveCookie = AnsiConsole.Prompt(new ConfirmationPrompt(
                "Do you want to save your cookies? [red]Warning: Your cookies would be stored as cleartext in the config file! The cookie can potentially be used to perform actions on your account without the need of a password[/]")
            .ShowChoices()
            .Yes('y').No('n')
        );

        AnsiConsole.MarkupLine(saveCookie ? "[green]Cookies saved![/]" : "[red]Cookies not saved![/]");

        return saveCookie;
    }

    private static string EnterPassword(out bool savePassword)
    {
        var pass = AnsiConsole.Prompt(new TextPrompt<string>("Please Enter your password for Last.fm:").Secret());

        savePassword = AnsiConsole.Prompt(new ConfirmationPrompt(
                "Do you want to save your password? [red]Warning: Your password would be stored as cleartext in the config file[/]")
            .ShowChoices()
            .Yes('y').No('n')
        );

        AnsiConsole.MarkupLine(savePassword ? "[green]Password saved![/]" : "[red]Password not saved![/]");


        return pass;
    }
}