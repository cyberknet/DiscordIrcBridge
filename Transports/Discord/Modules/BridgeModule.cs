using Discord;
using Discord.Interactions;
using DiscordIrcBridge.Configuration;
using Microsoft.Extensions.Logging;
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
    [Group("bridge","Commands related to the bridge functionality.")]
    public class BridgeModule : InteractionModuleBase<SocketInteractionContext>
    {
        // dependencies can be accessed through Property injection, public properties with public setters will be set by the service provider
        public InteractionService Commands { get; set; }

        private DiscordTransport _handler;
        private readonly MappingConfiguration _mappingConfiguration;
        private readonly IrcConfiguration _ircConfiguration;
        private readonly DiscordConfiguration _discordConfiguration;
        private readonly ILogger<BridgeModule> _log;

        public BridgeModule(DiscordTransport discordTransport, MappingConfiguration mappingConfiguration, IrcConfiguration ircConfiguration, DiscordConfiguration discordConfiguration, ILogger<BridgeModule> log)
        {
            this._handler = discordTransport;
            this._mappingConfiguration = mappingConfiguration;
            this._ircConfiguration = ircConfiguration;
            this._discordConfiguration = discordConfiguration;
            this._log = log;
        }

        
        [SlashCommand("status", "Returns the status of the Discord/IRC bridge")]
        public async Task Status()
        {
            var connected = _handler.Bridge?.IrcIsConnected ?? false;
            string status = connected ? "connected" : "not connected";
            Color color = connected ? Color.Green : Color.Red;

            var embedBuilder = new EmbedBuilder()
                .WithTitle("Bridge Status")
                .WithDescription($"Discord/IRC bridge is currently {status}")
                .WithColor(color)
                .WithCurrentTimestamp();
            await RespondAsync(embed: embedBuilder.Build());
        }

        [SlashCommand("reconnect", "Reconnects the IRC side of the Discord/IRC bridge")]
        public async Task Reconnect()
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle("Reconnecting Bridge")
                .WithDescription($"The IRC bridge is being reconnected.")
                .WithColor(Color.Orange)
                .WithCurrentTimestamp();
            await RespondAsync(embed: embedBuilder.Build());
            _handler.Bridge?.ReconnectIrc();
        }

        [SlashCommand("setup", "Sets the IRC configuration")]
        public async Task Setup(string serverName, int port, string nickname, string altNickname, string username, string realName)
        {
            if (!String.IsNullOrWhiteSpace(serverName))
                _ircConfiguration.Server = serverName;
            if (port > 0 && port < 65535)
                _ircConfiguration.Port = port;
            if (!string.IsNullOrWhiteSpace(nickname))
                _ircConfiguration.Nickname = nickname;
            if (!string.IsNullOrWhiteSpace(altNickname))
                _ircConfiguration.AlternateNickname = altNickname;
            if (!string.IsNullOrWhiteSpace(username))
                _ircConfiguration.Username = username;
            if (!string.IsNullOrWhiteSpace(realName))
                _ircConfiguration.RealName = realName;

            ConfigurationHelper.SaveMappingConfiguration(_mappingConfiguration);

            var embedBuilder = GetSettingsEmbed();
            await RespondAsync(embed: embedBuilder.Build());
        }

        [SlashCommand("settings", "Gets the IRC configuration")]
        public async Task Settings()
        {
            try
            {
                var embedBuilder = GetSettingsEmbed();
                await RespondAsync(embed: embedBuilder.Build());
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error building settings embed");
            }
        }

        private EmbedBuilder GetSettingsEmbed()
        {
            return new EmbedBuilder()
                .WithTitle("Updated IRC Settings")
                .AddField("Server Name", DefaultValue(_ircConfiguration.Server))
                .AddField("Port", DefaultValue(_ircConfiguration.Port, 1, 65535))
                .AddField("Nickname", DefaultValue(_ircConfiguration.Nickname))
                .AddField("Alternate Nickname", DefaultValue(_ircConfiguration.AlternateNickname))
                .AddField("Username", DefaultValue(_ircConfiguration.Username))
                .AddField("Real Name", DefaultValue(_ircConfiguration.RealName));
        }

        private string DefaultValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "*unset*";
            else
                return value;
        }

        private string DefaultValue(int value, int min, int max)
        {
            if (value >= min && value < max)
                return value.ToString();
            else
                return "*unset*";
        }

        [SlashCommand("list", "Shows the nicknames of users present on the IRC side of the link.")]
        public async Task List()
        {
            var discordChannel = this.Context.Channel;
            var mapping = _mappingConfiguration.Channels.FirstOrDefault(m => m.DiscordChannel != null && m.DiscordChannel.Id == discordChannel.Id);
            if (discordChannel == null || mapping == null || mapping.IrcChannel == null)
            {
                EmbedBuilder embed = new EmbedBuilder();
                // ephermal embed that says IRC channel not found
                await FollowupAsync(embed: embed.Build(), ephemeral: true);
            }
            else
            {
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("IRC Users")
                     .WithDescription("The following users are online in " + mapping?.IrcChannel?.Name);
                StringBuilder discord = new StringBuilder();
                StringBuilder irc = new StringBuilder();
                foreach(var channelUser in mapping?.IrcChannel?.Users)
                {
                    var userMap = _mappingConfiguration.Users.FirstOrDefault(m => m.IrcNickname.ToLower().Trim() == channelUser.User.NickName.ToLower().Trim());
                    if (userMap != null)
                        discord.Append($"\r\n* @<{userMap.DiscordUserId}> ({channelUser.User.NickName})");
                    else
                        irc.Append($"\r\n* {channelUser.User.NickName}");

                }
                if (discord.Length == 0)
                    embed.AddField("Discord Users", "None");
                else
                    embed.AddField("Discord Users", discord.ToString().Substring(2));

                if (irc.Length == 0)
                    embed.AddField("IRC Only Users", "None");
                else
                    embed.AddField("IRC Only Users", irc.ToString().Substring(2));
                
                await RespondAsync(embed: embed.Build());
                
            }
            
        }

        
    }
}
