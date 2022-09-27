using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Configuration
{
    public class ConfigurationHelper
    {
        private const string DATA_DIRECTORY = "/data/";
        private const string CONFIG_MAPPING = "mapping.json";
        private const string CONFIG_IRC = "irc.json";
        private const string CONFIG_DISCORD = "discord.json";
        private const string CONFIG_STATISTICS = "statistics.json";

        public static void SaveMappingConfiguration(MappingConfiguration config)
        {
            SaveConfigurationFile(config, CONFIG_MAPPING);
        }
        public static void SaveDiscordConfiguration(DiscordConfiguration config)
        {
            SaveConfigurationFile(config, CONFIG_DISCORD);
        }
        public static void SaveIrcConfiguration(IrcConfiguration config)
        {
            SaveConfigurationFile(config, CONFIG_IRC);
        }

        public static void SaveStatistics(Statistics statistics, bool shuttingDown)
        {
            if (shuttingDown)
            {
                statistics.PreviousUptime = statistics.PreviousUptime + (DateTime.Now - statistics.LastStartedAt);
            }
            SaveConfigurationFile(statistics, CONFIG_STATISTICS);
        }

        public static MappingConfiguration LoadMappingConfiguration()
        {
            var config = LoadConfigurationFile<MappingConfiguration>(CONFIG_MAPPING);
            if (config == null)
                config = new();
            return config;
        }
       
        public static IrcConfiguration LoadIrcConfiguration()
        {
            var config = LoadConfigurationFile<IrcConfiguration>(CONFIG_IRC);
            if (config == null)
                config = new IrcConfiguration();
            return config;
        }
        public static DiscordConfiguration LoadDiscordConfiguration()
        {
            var config = LoadConfigurationFile<DiscordConfiguration>(CONFIG_DISCORD);
            if (config == null)
            {
                config = new();
                var env = Environment.GetEnvironmentVariables();
                // check for Guild Id and Token in environment variables
                if (env != null)
                {
                    string? str = string.Empty;
                    if (env.Contains("DISCORD_GUILDID"))
                    {
                        str = env["DISCORD_GUILDID"] as string;
                        if (str != null)
                        {
                            if (ulong.TryParse(str, out var guildId))
                            {
                                config.GuildId = guildId;
                            }
                        }
                    }

                    if (env.Contains("DISCORD_TOKEN"))
                    {
                        str = env["DISCORD_TOKEN"] as string;
                        if (str != null)
                        {
                            config.Token = str;
                        }
                    }

                    if (env.Contains("DISCORD_COMMANDPREFIX"))
                    {
                        str = env["DISCORD_COMMANDPREFIX"] as string;
                        if (str != null)
                        {
                            config.CommandPrefix = str[0];
                        }
                    }
                }
            }
            return config;
        }

        public static Statistics LoadStatistics()
        {
            var config = LoadConfigurationFile<Statistics>(CONFIG_STATISTICS);
            if (config == null)
            { 
                config = new Statistics();
                config.PreviousUptime = TimeSpan.Zero;
            }
            config.LastStartedAt = DateTime.Now;
            return config;
        }
        private static T? LoadConfigurationFile<T>(string filename) where T : class
        {
            if (Directory.Exists(DATA_DIRECTORY))
            {
                var configFile = Path.Combine(DATA_DIRECTORY, filename);
                if (File.Exists(configFile))
                {
                    try
                    {
                        using Stream stream = new FileStream(configFile, FileMode.Open, FileAccess.Read);
                        var config = System.Text.Json.JsonSerializer.Deserialize<T>(stream);
                        return config;
                    }
                    catch(Exception ex)
                    {
                    }
                }
            }
            return null;
        }

        private static void SaveConfigurationFile<T>(T config, string filename)
        {
            try
            {
                if (Directory.Exists(DATA_DIRECTORY))
                {
                    var configFile = Path.Combine(DATA_DIRECTORY, filename);
                    JsonSerializerOptions options = new()
                    {
                        WriteIndented = true
                    };
                    string json = System.Text.Json.JsonSerializer.Serialize(config, options);
                    System.IO.File.WriteAllText(configFile, json);
                }
            }
            catch (Exception) { } // what's the worst that can happen?
        }
    }
}
