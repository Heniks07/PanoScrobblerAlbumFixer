using System.Runtime.InteropServices;
using Spectre.Console;
using YamlDotNet.Serialization;

namespace PanoScrobblerAlbumFixer;

public static partial class Program
{
    private static void ConfigFileCheck()
    {
        if (!Directory.Exists(ConfigPath) || !File.Exists(ConfigPath + "/config.yaml"))
        {
            Directory.CreateDirectory(ConfigPath);

            AnsiConsole.MarkupLine($"[bold red]Config file not found![/] Creating one in {ConfigPath}");

            // Create the config file

            // Ask for the API key

            var input = AnsiConsole.Prompt(
                new TextPrompt<string>(
                    "Please enter your Last.fm API key or press Enter to use the one provided by me:").AllowEmpty());
            var apiKey = string.IsNullOrEmpty(input)
                ? "a08dabd1bd340cafc153d1ade9bfa4b7"
                : input;
            // Ask for the API secret
            input = AnsiConsole.Prompt(
                new TextPrompt<string>(
                    "Please enter your Last.fm API secret or press Enter to use the one provided by me:").AllowEmpty());
            var apiSecret = string.IsNullOrEmpty(input)
                ? "4af271b8175b7f5e78dd462ca25bab91"
                : input;

            var user = LoginGetUser(apiKey, apiSecret, out var savePassword);


            var config = new Configuration
            {
                ApiKey = apiKey,
                ApiSecret = apiSecret,
                Domain = "https://www.last.fm",
                ScrobblerDomain = "https://ws.audioscrobbler.com/2.0",
                User = user
            };
            _config = config;

            WriteConfig(savePassword);
        }
        else
        {
            AnsiConsole.MarkupLine("[bold green]Config file found![/]");
            ReadConfig();
        }
    }

    private static void ReadConfig()
    {
        var yaml = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
        var config = yaml.Deserialize<Configuration>(File.ReadAllText(ConfigPath + "/config.yaml"));
        _config = config;
    }

    private static void WriteConfig(bool savePassword = true, bool saveCookie = true)
    {
        var yaml = new SerializerBuilder().Build();
        if (!savePassword && _config != null)
        {
            _config.User.Password = null;
        }

        if (!saveCookie && _config != null)
        {
            _config.User.SessionId = null;
            _config.User.CsrfToken = null;
        }

        var yamlString = yaml.Serialize(_config);

        File.WriteAllText(ConfigPath + "/config.yaml", yamlString);
    }

    private static string ConfigPath => RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
          "/.config/PanoScrobblerAlbumFixer"
        : Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) +
          @"\PanoScrobblerAlbumFixer";
}