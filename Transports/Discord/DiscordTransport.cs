using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Net;
using Discord.Webhook;
using Discord.WebSocket;
using DiscordIrcBridge.Configuration;
using DiscordIrcBridge.Messages;
using DiscordIrcBridge.Transports.Irc.Formatting;
using IrcDotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Transports.Discord
{
    public class DiscordTransport : TransportBase
    {
        private readonly DiscordConfiguration _configuration;
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactionService;
        private readonly MappingConfiguration _mappingConfiguration;
        private readonly IServiceProvider _services;
        private bool _connectMessageSent = false;
        private readonly ILogger<DiscordTransport> _log;
        private SocketGuild? _guild = null;
        private readonly Statistics _statistics;

        public DiscordTransport(DiscordConfiguration discordConfiguration, MappingConfiguration mappingConfiguration, DiscordSocketClient client, IServiceProvider services, InteractionService interactionService, ILogger<DiscordTransport> log, Statistics statistics)
        {
            _configuration = discordConfiguration;
            _client = client;
            _services = services;
            _interactionService = interactionService;
            _mappingConfiguration = mappingConfiguration;
            _log = log;
            _statistics = statistics;

            _client.Log += Client_LogAsync;
            _client.Ready += Client_ReadyAsync;
            _client.MessageReceived += Client_MessageReceived;
        }

        private async Task Client_MessageReceived(SocketMessage messageParam)
        {
            // don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // if the message starts with our command prefix, don't broadcast
            if (message.HasCharPrefix(_configuration.CommandPrefix, ref argPos)) return;

            // if the message is a mention prefix for the bot, don't respond
            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                return;
            // if the author was a bot, don't respond
            if (message.Author.IsBot)
                return;

            IUserMessage msg = message;
            string content = msg.Content;

            var imessage = await message.Channel.GetMessageAsync(message.Id);
            var guildUser = msg.Author as SocketGuildUser;

            if (guildUser != null && imessage != null)
            {
                _log.LogDebug($"Discord {guildUser.Guild.Name}/{guildUser.DisplayName}: {imessage.Content}");

                TextMessage textMessage = new TextMessage();
                textMessage.Channel = message.Channel.Name;
                textMessage.Text = imessage.CleanContent;
                textMessage.User = guildUser.DisplayName;
                if (!_statistics.ChannelMessages.ContainsKey(message.Channel.Id))
                    _statistics.ChannelMessages.Add(message.Channel.Id, 1);
                else
                    _statistics.ChannelMessages[message.Channel.Id] += 1;
                if (!String.IsNullOrWhiteSpace(imessage.CleanContent))
                {
                    Bridge?.Broadcast(this, textMessage);
                }
                if (imessage.Attachments.Count > 0)
                {
                    foreach (var attachment in imessage.Attachments)
                    {
                        var contentType = attachment.ContentType.ToLower();
                        AttachmentMessage imageMessage = new AttachmentMessage();
                        imageMessage.Channel = message.Channel.Name;
                        imageMessage.Text = attachment.Url;
                        imageMessage.User = guildUser.DisplayName;
                        Bridge?.Broadcast(this, imageMessage);
                    }
                }
            }
            
        }

        private async Task Client_LogAsync(LogMessage log) => Console.WriteLine(log);
        

        private async Task Client_ReadyAsync()
        {
            _log.LogInformation("Discord Ready");
            if (Program.IsDebug())
            {
                await _interactionService.RegisterCommandsToGuildAsync(_configuration.GuildId);
            }
            else
            {
                // this can take up to an hour to complete
                await _interactionService.RegisterCommandsGloballyAsync(true);
            }

            _guild = _client.GetGuild(_configuration.GuildId);

            var discordChannels = _guild.Channels.ToList();

            // get the Discord channel webooks and references
            foreach (var channel in _mappingConfiguration.Channels)
            {
                if (!string.IsNullOrWhiteSpace(channel.DiscordWebHook))
                {
                    channel.DiscordWebHookClient = new DiscordWebhookClient(channel.DiscordWebHook);
                }
                channel.DiscordChannel = discordChannels.FirstOrDefault(dc => dc.Id == channel.DiscordChannelId);
            }

            
            if (Bridge.IrcIsConnected && !_connectMessageSent)
            {
                OnMessageBroadcast(this, new ConnectMessage());
            }

        }

        private async Task Client_HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
                var context = new SocketInteractionContext(_client, interaction);

                // Execute the incoming command.
                var result = await _interactionService.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess)
                    switch (result.Error)
                    {
                        case InteractionCommandError.UnmetPrecondition:
                            // implement
                            break;
                        default:
                            break;
                    }
            }
            catch
            {
                // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        protected override async void OnMessageBroadcast(TransportBase source, MessageBase message)
        {
            if (message is TextMessage textMessage)
            {
                await SendMessageToDiscord(textMessage);
            }
            else 
            {
                await SendEventToDiscord(message);
            }
        }

        private async Task SendEventToDiscord(MessageBase message)
        {
            EmbedBuilder eventEmbed = GetEventEmbedBuilder(message);

            if (message is ChannelMessage channelMessage)
            {
                var channelMapping = _mappingConfiguration.Channels.FirstOrDefault(m => m.IrcChannelName.ToLower().Trim() == channelMessage.Channel.ToLower().Trim());
                if (channelMapping != null)
                {
                    bool success = await SendEventEmbed(eventEmbed, channelMapping);

                }
            }
            else
            {
                bool success = true;
                foreach(var channelMapping in _mappingConfiguration.Channels)
                {
                    success &= await SendEventEmbed(eventEmbed, channelMapping);
                }
                if (message is ConnectMessage)
                    _connectMessageSent = success;
            }
        }

        private async Task<bool> SendEventEmbed(EmbedBuilder eventEmbed, ChannelMapping channelMapping)
        {

            var socketChannel = channelMapping.DiscordChannel;
            var webhookClient = channelMapping.DiscordWebHookClient;
            if (socketChannel != null)
            {
                var perms = socketChannel.GetPermissionOverwrite(_client.CurrentUser);
                var canPingEveryone = (perms != null && perms.Value.MentionEveryone == PermValue.Allow);
                var allowedMentions = canPingEveryone ? AllowedMentions.All : AllowedMentions.None;

                var imc = socketChannel as IMessageChannel;
                if (imc != null)
                {
                    await imc.SendMessageAsync(embed: eventEmbed.Build(), allowedMentions: allowedMentions);
                    return true;
                }
            }
            return false;
        }

        private EmbedBuilder GetEventEmbedBuilder(MessageBase message)
        {
            EmbedBuilder embed = new EmbedBuilder();
            string title = "Event Title";
            string description = "Default description.";
            Color color = Color.Default;
            if (message is ConnectMessage connect)
            {
                title = "IRC Connected";
                description = "Connection to IRC has been re-established";
                color = Color.Green;
            }
            else if (message is DisconnectMessage disconnect)
            {
                title = "IRC Disconnected";
                description = "Connection to IRC was lost";
                color = Color.Red;
            }
            else if (message is ChannelMessage channelMessage)
            {
                string nickname = channelMessage.User;
                var mapping = _mappingConfiguration.Channels.FirstOrDefault(m => m.IrcChannelName.ToLower().Trim() == channelMessage.Channel.ToLower().Trim());
                if (mapping != null)
                {
                    var socketGuildChanel = mapping.DiscordChannel;
                    if (socketGuildChanel != null)
                    {
                        var user = GetDiscordUser(channelMessage.User, socketGuildChanel);
                        if (user != null)
                        {
                            nickname = $"<@{user.Id}> ({nickname})";
                        }
                        string channelName = channelMessage.Channel;

                        if (message is JoinMessage join)
                        {
                            description = $"{nickname} has joined {channelName}";
                        }
                        else if (message is PartMessage part)
                        {
                            description = $"{nickname} has left {channelName}";
                        }
                        else if (message is KickMessage kick)
                        {
                            description = $"{nickname} has been kicked from {channelName}";
                            if (!string.IsNullOrEmpty(kick.Text))
                                embed.AddField("Reason", kick.Text);
                        }
                        else if (message is QuitMessage quit)
                        {
                            description = $"{nickname} has Quit IRC";
                            if (!string.IsNullOrEmpty(quit.Text))
                                embed.AddField("Reason", quit.Text);
                            color = Color.Blue;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(title))
                embed.WithTitle(title);
            if (!string.IsNullOrWhiteSpace(description))
                embed.WithDescription(description);
            if (color != Color.Default)
                embed.WithColor(color);
            return embed;
        }

        private async Task SendMessageToDiscord(TextMessage message)
        {
            // get channel mapping
            var channelMapping = _mappingConfiguration.Channels.FirstOrDefault(m => m.IrcChannelName.ToLower().Trim() == message.Channel.ToLower().Trim());
            if (channelMapping != null)
            {
                // convert irc text to markdown
                string text = IrcStringToMarkdownString(message.Text);
                if (!_statistics.ChannelMessages.ContainsKey(channelMapping.DiscordChannelId))
                    _statistics.ChannelMessages.Add(channelMapping.DiscordChannelId, 1);
                else
                    _statistics.ChannelMessages[channelMapping.DiscordChannelId] += 1;

                bool sent = false;
                var socketChannel = channelMapping.DiscordChannel;
                var webhookClient = channelMapping.DiscordWebHookClient;

                // webhook first
                var perms = socketChannel.GetPermissionOverwrite(_client.CurrentUser);
                var canPingEveryone = perms != null && (perms.Value.MentionEveryone == PermValue.Allow);
                var allowedMentions = canPingEveryone ? AllowedMentions.All : AllowedMentions.None;

                var mappedUser = GetDiscordUser(message.User, channelMapping.DiscordChannel);
                // can't post to webhook if no discord user available
                if (mappedUser != null && webhookClient != null)
                {
                    var avatarUrl = mappedUser.GetAvatarUrl(format: ImageFormat.Png, size: 128);
                    await webhookClient.SendMessageAsync(
                        text: text,
                        username: mappedUser.Username,
                        avatarUrl: avatarUrl,
                        allowedMentions: allowedMentions);
                    sent = true;
                }

                // if it didn't get sent via webhook, send it the normal way
                if (!sent)
                {
                    var imc = socketChannel as IMessageChannel;
                    string username = mappedUser != null ? mappedUser.Username : message.User;
                    string messageText =$"<{username}> {text}";
                    if (imc != null)
                    {
                        await imc.SendMessageAsync(text: messageText, allowedMentions: allowedMentions);
                    }
                }
                
            }
        }

        private string IrcStringToMarkdownString(string text)
        {
            IrcFormatting formatting = new IrcFormatting();
            var blocks = formatting.Parse(text);
            // some IRC clients do not use reverse
            blocks.ForEach(b => b.Italic = b.Italic || b.Reverse);
            var mdText = new StringBuilder();
            for(int i = 0; i < blocks.Count; i++)
            {
                // Default to unstyled blocks when index out of range
                var block = blocks[i];
                var prevBlock = i > 0 ? blocks[i - 1] : new IrcBlock(null);
                // if the formatting carries over from one block to the next, we will need an added space to make Markdown display correctly
                if ((block.Bold && prevBlock.Bold) || (block.Italic && prevBlock.Italic) || (block.Underline && prevBlock.Underline))
                {
                    mdText.Append(' ');
                }
                // add start markers when style turns from false to true
                if (block.Italic) mdText.Append("*");
                if (block.Bold) mdText.Append("**");
                if (block.Underline) mdText.Append("__");
                mdText.Append(block.Text);
                if (block.Underline) mdText.Append("__");
                if (block.Bold) mdText.Append("**");
                if (block.Italic) mdText.Append("*");
            }
            return mdText.ToString();
        }

        private SocketUser GetDiscordUser(string nickname, SocketGuildChannel channel)
        {
            var user = _mappingConfiguration.Users.FirstOrDefault(u => u.IrcNickname.ToLower().Trim() == nickname.ToLower().Trim());
            if (user != null)
            {
                var socketUser = _client.GetUser(user.DiscordUserId);
                if (socketUser != null)
                    return socketUser;
                else
                {
                    var guildUser = channel.Users.FirstOrDefault(u => u.Id == user.DiscordUserId);
                    if (guildUser != null && guildUser is SocketUser su)
                    {
                        return su;
                    }
                }
            }
            return null;

        }


        internal override async Task Initialize(Bridge bridge)
        {
            await base.Initialize(bridge);
            if (ConfigurationValid())
            {
                // add any public command modules
                await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

                // Process the InteractionCreated payloads to execute Interactions commands
                _client.InteractionCreated += Client_HandleInteraction;

                // log in and start the bot
                await _client.LoginAsync(TokenType.Bot, _configuration.Token);
                await _client.StartAsync();
            }
            else
            {
                _log.LogCritical("Not connecting to Discord because of invalid configuration");
            }
        }

        private bool ConfigurationValid()
        {
            _log.LogInformation($"Validating connection for GuildId {_configuration.GuildId} and Token {_configuration.Token}");
            return
                _configuration.GuildId > 1 &&
                !string.IsNullOrWhiteSpace(_configuration.Token);
        }

    }
}
