using System.Runtime.InteropServices;
using YamlDotNet.Serialization;

namespace PanoScrobblerAlbumFixer;

public static partial class Program
{
    private static void ConfigFileCheck()
    {
        if (!Directory.Exists(ConfigPath) || !File.Exists(ConfigPath + "/config.yaml"))
        {
            Directory.CreateDirectory(ConfigPath);
            Console.WriteLine("Config file not found, creating one in {0}", ConfigPath);

            // Create the config file

            // Ask for the API key
            Console.Write("Please enter your Last.fm API key or press Enter to use the one provided by me: ");
            var input = Console.ReadLine();
            var apiKey = string.IsNullOrEmpty(input)
                ? "a08dabd1bd340cafc153d1ade9bfa4b7"
                : input;
            // Ask for the API secret
            Console.Write("Please enter your Last.fm API secret or press Enter to use the one provided by me: ");
            input = Console.ReadLine();
            var apiSecret = string.IsNullOrEmpty(input)
                ? "4af271b8175b7f5e78dd462ca25bab91"
                : input;


            var config = new Configuration
            {
                ApiKey = apiKey,
                ApiSecret = apiSecret
            };
            _config = config;

            WriteConfig();
        }
        else
        {
            Console.WriteLine("Config file found in {0}", ConfigPath);
            ReadConfig();
        }
    }

    private static void ReadConfig()
    {
        var yaml = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
        var config = yaml.Deserialize<Configuration>(File.ReadAllText(ConfigPath + "/config.yaml"));
        _config = config;
    }

    private static void WriteConfig()
    {
        var yaml = new SerializerBuilder().Build();
        var yamlString = yaml.Serialize(_config);
        File.WriteAllText(ConfigPath + "/config.yaml", yamlString);
    }

    private static string ConfigPath => RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
          "/.config/PanoScrobblerAlbumFixer"
        : Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) +
          @"\PanoScrobblerAlbumFixer";
}