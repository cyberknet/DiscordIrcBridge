using Discord.Interactions;
using Discord.WebSocket;
using DiscordIrcBridge.Configuration;
using DiscordIrcBridge.Transports.Discord;
using DiscordIrcBridge.Transports.Irc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.Discord;
using Serilog.Sinks.SystemConsole.Themes;

namespace DiscordIrcBridge
{
    public class Program
    {



        public static Task Main(string[] args) => new Program().MainAsync(args);

        public async Task MainAsync(string[] args)
        {
            var configBuilder = BuildConfiguration();
            if (configBuilder != null)
            {
                var configuration = configBuilder.Build();

                LoggingLevelSwitch lls = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Debug);
                var discordConfig = ConfigurationHelper.LoadDiscordConfiguration();

                var logConfig = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(lls)
                    .ReadFrom.Configuration(configuration);
                //.WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                //.WriteTo.File("/data/{Date}-discordircbot2.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);
                //if (discordConfig.DebugChannelWebhookId.HasValue && !string.IsNullOrWhiteSpace(discordConfig.DebugChannelWebhookToken))
                //{
                //    logConfig.WriteTo.Discord(discordConfig.DebugChannelWebhookId.Value, discordConfig.DebugChannelWebhookToken);
                //}
                //logConfig.Enrich.FromLogContext();

                Log.Logger = logConfig.CreateLogger();
                Log.Logger.Information("Starting Up");

                using IHost host =
                    Host
                    .CreateDefaultBuilder()
                    .ConfigureAppConfiguration(builder =>
                    {
                        BuildConfiguration(builder);
                    })
                    .ConfigureServices((builder, services) =>
                            ConfigureServices(builder.Configuration, services, lls)
                        )
                .UseSerilog()
                .Build();

                var bridge = host.Services.GetRequiredService<Bridge>();
                bridge.Initialize();

                await host.RunAsync();
            }
        }

        private static IConfigurationBuilder BuildConfiguration(IConfigurationBuilder? builder = null)
        {
            try
            {
                if (builder == null)
                    builder = new ConfigurationBuilder();
                builder
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                    //.AddJsonFile("/data/settings.json", optional: true, reloadOnChange: false);
                return builder;
            }
            catch (Exception ex)
            {

            }
            return null;
        }

        private IServiceCollection ConfigureServices(IConfiguration configuration, IServiceCollection? services = null, LoggingLevelSwitch logLevelSwitch = null)
        {

            if (services == null)
                services = new ServiceCollection();

            var config = configuration.Get<ConfigurationHelper>();
            if (config == null)
            {
                config = new ConfigurationHelper();
            }
            var mapping = ConfigurationHelper.LoadMappingConfiguration();
            var discord = ConfigurationHelper.LoadDiscordConfiguration();
            var irc = ConfigurationHelper.LoadIrcConfiguration();
            var statistics = ConfigurationHelper.LoadStatistics();

            //services.AddSingleton<ConfigurationFactory>();
            services.AddSingleton<Statistics>(statistics);
            services.AddSingleton<Bridge>();
            //services.AddSingleton<MappingConfiguration>(serviceProvider =>
            //            serviceProvider.GetRequiredService<ConfigurationFactory>().Mapping
            //        );
            services.AddSingleton<MappingConfiguration>(mapping);

            // IRC CLASSES
            //services.AddSingleton<IrcConfiguration>(serviceProvider =>
            //            serviceProvider.GetRequiredService<ConfigurationFactory>().Irc
            //        );
            services.AddSingleton<IrcConfiguration>(irc);
            services.AddSingleton<IrcTransport>();
            services.AddTransient<BridgeConnection>();


            // DISCORD CLASSES
            //services.AddSingleton<DiscordConfiguration>(serviceProvider =>
            //    serviceProvider.GetRequiredService<ConfigurationFactory>().Discord
            //);
            services.AddSingleton<DiscordConfiguration>(discord);
            services.AddSingleton<DiscordTransport>();
            services.AddSingleton<DiscordSocketConfig>();
            services.AddSingleton<DiscordSocketConfig>(sp =>
            {
                var cfg = new DiscordSocketConfig();
                cfg.GatewayIntents = Discord.GatewayIntents.AllUnprivileged | Discord.GatewayIntents.MessageContent | Discord.GatewayIntents.GuildMembers;
                cfg.AlwaysDownloadUsers = true;
                return cfg;
            });
            services.AddSingleton<DiscordSocketClient>();
            services.AddSingleton<InteractionService>();

            if (logLevelSwitch != null)
                services.AddSingleton(logLevelSwitch);

            return services;
        }




        public static bool IsDebug()
        {
#if DEBUG
            return true;
#else
                return false;
#endif
        }
    }
}