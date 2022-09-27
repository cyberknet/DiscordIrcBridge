using Discord;
using Discord.Interactions;
using Discord.Webhook;
using Discord.WebSocket;
using DiscordIrcBridge.Configuration;
using DiscordIrcBridge.Messages;
using IrcDotNet;
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
    [Group("channel", "Commands related to the channel functionality.")]
    public class ChannelModule : InteractionModuleBase<SocketInteractionContext>
    {
        // dependencies can be accessed through Property injection, public properties with public setters will be set by the service provider
        public InteractionService Commands { get; set; }

        private DiscordTransport _handler;
        private DiscordSocketClient _client;
        private MappingConfiguration _mappingConfiguration;

        public ChannelModule(DiscordTransport discordTransport, DiscordSocketClient client, MappingConfiguration mappingConfiguration)
        {
            _handler = discordTransport;
            _client = client;
            _mappingConfiguration = mappingConfiguration;
        }

        [SlashCommand("setwebook", "Sets the webhook URL for a mapped channel")]
        public async Task SetWebhook(IGuildChannel channel, string webhookUrl)
        {
            var mapping = _mappingConfiguration.Channels.FirstOrDefault(m => m.DiscordChannelId == channel.Id);
            var embedBuilder = new EmbedBuilder();
            if (mapping == null)
            {
                embedBuilder.WithTitle($"Error setting webhook for #{channel.Name}")
                    .WithDescription("The channel provided is not mapped to an IRC channel.")
                    .WithColor(Color.Red);
            }
            else
            {
                mapping.DiscordWebHook = webhookUrl;
                mapping.DiscordWebHookClient = new DiscordWebhookClient(webhookUrl);
                ConfigurationHelper.SaveMappingConfiguration(_mappingConfiguration);
                embedBuilder.WithTitle($"Channel Update for #{channel.Name}")
                    .WithDescription($"The webhook for #{channel.Name} has been updated.")
                    .WithColor(Color.Green);
            }
            await RespondAsync(embed: embedBuilder.Build());
        }



        [SlashCommand("map", "Maps a discord username to an IRC username.")]
        [RequireRole("Bridge Admin")]
        public async Task Map(IChannel channel, string ircChannel, string webhook = "")
        {
            bool added = false;
            var exists = _mappingConfiguration.Channels.Count(m => m.DiscordChannelId == channel.Id && ircChannel.ToLower().Trim() == m.IrcChannelName.ToLower().Trim()) > 0;
            if (!exists)
            {
                if (channel is SocketGuildChannel socketGuildChannel)
                {
                    var mapping = new ChannelMapping
                    {
                        IrcChannelName = ircChannel,
                        DiscordChannelId = channel.Id,
                        DiscordChannel = socketGuildChannel
                    };
                    if (!string.IsNullOrWhiteSpace(webhook))
                    {
                        mapping.DiscordWebHook = webhook;
                        mapping.DiscordWebHookClient = new DiscordWebhookClient(webhook);
                    }
                    _mappingConfiguration.Channels.Add(mapping);
                    _handler.Bridge?.Broadcast(_handler, new MapChannelMessage { ChannelMapping = mapping });
                    await RespondAsync($"Discord channel <@{channel.Id}> is now mapped to IRC channel {ircChannel}");
                }
                else
                {
                    await RespondAsync($"An error occurred while mapping <@{channel.Id}> to IRC channel {ircChannel}");
                }
            }
            else
            {
                await RespondAsync($"Discord channel <@{channel.Id} is already mapped to IRC user {ircChannel}");
            }
        }

        [SlashCommand("unmap", "Removes Discord/IRC channel mapping")]
        [RequireRole("Bridge Admin")]
        public async Task Unmap(IChannel channel, string ircChannel)
        {
            var mappings = _mappingConfiguration.Channels.Where(m => m.DiscordChannelId == channel.Id && ircChannel.ToLower().Trim() == m.IrcChannelName.ToLower().Trim());
            if (mappings != null)
            {
                foreach (var mapping in mappings)
                {

                }
            }
        }

        [SlashCommand("ignore", "Ignores a user in IRC channels mapped for the discord channel provided.")]
        [RequireRole("Bridge Admin")]
        public async Task Ignore(IChannel channel, string ignoreNickname)
        {
            foreach(var mapping in _mappingConfiguration.Channels)
            {
                if (mapping.DiscordChannelId == channel.Id)
                {
                    if (!mapping.IgnoreUsers.Contains(ignoreNickname, StringComparer.OrdinalIgnoreCase))
                    {
                        mapping.IgnoreUsers.Add(ignoreNickname);
                    }
                }
            }
            ConfigurationHelper.SaveMappingConfiguration(_mappingConfiguration);
            await ReplyAsync($"IRC user {ignoreNickname} is now ignored in all channels mapped for #{channel.Name}");
        }
    }
}
