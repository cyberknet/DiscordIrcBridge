using Discord;
using Discord.Interactions;
using DiscordIrcBridge.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Discord.Color;

namespace DiscordIrcBridge.Transports.Discord.Modules
{
    // interaction modules must be public and inherit from an IInteractionModuleBase
    [Group("info", "Information and debugging commands.")]
    public class InfoModule : InteractionModuleBase<SocketInteractionContext>
    {
        // dependencies can be accessed through Property injection, public properties with public setters will be set by the service provider
        public InteractionService Commands { get; set; }

        private DiscordTransport _handler;
        private readonly MappingConfiguration _mappingConfiguration;
        private readonly IrcConfiguration _ircConfiguration;
        private readonly DiscordConfiguration _discordConfiguration;
        private readonly ILogger<BridgeModule> _log;
        private readonly LoggingLevelSwitch _loggingLevelSwitch;
        private readonly Statistics _statistics;

        public InfoModule(DiscordTransport discordTransport, MappingConfiguration mappingConfiguration, IrcConfiguration ircConfiguration, DiscordConfiguration discordConfiguration, ILogger<BridgeModule> log, LoggingLevelSwitch loggingLevelSwitch, Statistics statistics)
        {
            this._handler = discordTransport;
            this._mappingConfiguration = mappingConfiguration;
            this._ircConfiguration = ircConfiguration;
            this._discordConfiguration = discordConfiguration;
            this._log = log;
            this._loggingLevelSwitch = loggingLevelSwitch;
            this._statistics = statistics;
        }

        [SlashCommand("debugchannel", "Sets the active debug channel")]
        [RequireRole("Bridge Admin")]
        public async Task DebugChannel(string webhookId, string token)
        {
            if (!string.IsNullOrWhiteSpace(webhookId) && !string.IsNullOrWhiteSpace(token))
            {
                var embedBuilder = new EmbedBuilder();
                if (!ulong.TryParse(webhookId, out var id))
                {
                    embedBuilder
                        .WithTitle("Debug Channel Not Changed")
                        .WithDescription($"The debug channel webook provided ({webhookId}) is not a valid webhook id.");
                }
                else
                {
                    _discordConfiguration.DebugChannelWebhookId = id;
                    _discordConfiguration.DebugChannelWebhookToken = token;

                    ConfigurationHelper.SaveDiscordConfiguration(_discordConfiguration);

                    embedBuilder
                        .WithTitle("Debug Channel Changed")
                        .WithDescription($"The debug channel webook has been changed to {webhookId}, this change will not take effect until the application restarts.");
                    await RespondAsync(embed: embedBuilder.Build());
                }
                await RespondAsync(embed: embedBuilder.Build());
            }
        }

        [SlashCommand("statistics", "Displays various bot statistics")]
        public async Task Statistics()
        {
            var connected = _handler.Bridge?.IrcIsConnected ?? false;
            string status = connected ? "connected" : "not connected";
            int messageCount = 0;
            if (_statistics.ChannelMessages.Count > 0)
                messageCount = _statistics.ChannelMessages.Values.Sum();
            var fields = new List<EmbedFieldBuilder>();
            fields.Add(CreateFieldEmbed("Last Started", _statistics.LastStartedAt));
            fields.Add(CreateFieldEmbed("Uptime", (DateTime.Now - _statistics.LastStartedAt).ToPrintableString()));
            fields.Add(CreateFieldEmbed("Messages", messageCount));
            fields.Add(CreateFieldEmbed("Commands", _statistics.CommandsProcessed));
            fields.Add(CreateFieldEmbed("Disconnections", _statistics.IrcDisconnections));

            var embedBuilder = new EmbedBuilder()
                .WithTitle("Bridge Statistics")
                .WithDescription($"Discord/IRC bridge is currently {status}")
                .WithFields(fields);
            

            await RespondAsync(embed: embedBuilder.Build());
        }

        private EmbedFieldBuilder CreateFieldEmbed(string title, object value, bool inline = false)
        {
            return new EmbedFieldBuilder()
                .WithName(title)
                .WithValue(value)
                .WithIsInline(inline);
        }
    }
}
