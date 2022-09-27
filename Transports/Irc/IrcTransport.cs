using Discord;
using Discord.Rest;
using DiscordIrcBridge.Configuration;
using DiscordIrcBridge.Messages;
using IrcDotNet;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Transports.Irc
{
    public class IrcTransport : TransportBase
    {
        private readonly IrcConfiguration _configuration;
        private readonly MappingConfiguration _mappingConfiguration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IrcTransport> _log;
        private readonly Statistics _statistics;
        private int _maximumLineLength = 425;

        public List<IrcUser> _trackedUsers = new();
        // Internal and exposable collection of all clients that communicate individually with servers.
        public BridgeBot? BridgeBot { get; private set; }

        public IrcTransport(IrcConfiguration ircConfiguration, MappingConfiguration mappingConfiguration, IServiceProvider serviceProvider, ILogger<IrcTransport> log, Statistics statistics)
        {
            _configuration = ircConfiguration;
            _mappingConfiguration = mappingConfiguration;
            _serviceProvider = serviceProvider;
            _log = log;
            _statistics = statistics;
        }

        public void Connect()
        {
            _log.LogInformation($"Connecting to IRC server {_configuration.Server}:{_configuration.Port}");
            var registrationInfo = new BridgeBotRegistration
            {
                NickName = _configuration.Nickname,
                Password = _configuration.Password
            };

            BridgeBot = _serviceProvider.GetRequiredService<BridgeBot>();
            BridgeBot.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
            BridgeBot.Connected += BridgeBot_Connected;
            BridgeBot.Disconnected += BridgeBot_Disconnected;
            BridgeBot.Registered += BridgeBot_Registered;
            BridgeBot.ServerSupportedFeaturesReceived += BridgeBot_ServerSupportedFeaturesReceived;
            BridgeBot.Error += BridgeBot_Error;
            BridgeBot.ProtocolError += BridgeBot_ProtocolError;

            using (var connectedEvent = new ManualResetEventSlim(false))
            {
                // following line makes sure that if we connect we don't dispose the bot!
                BridgeBot.Connected += (s2, e2) => connectedEvent.Set();

                BridgeBot.Connect();
                if (!connectedEvent.Wait(10000))
                {
                    _log.LogInformation("Timeout while connecting to IRC server");
                    BridgeBot.Dispose();
                    ConsoleUtilities.WriteError("Connection to '{0}' timed out.", _configuration.Server);
                    return;
                }
            }
        }

        private void BridgeBot_ProtocolError(object? sender, IrcProtocolErrorEventArgs e)
        {
            switch (e.Code)
            {
                case 433: // Nickname in use
                    BridgeBot.LocalUser.SetNickName(_configuration.AlternateNickname);
                    break;
                default:
                    _log.LogCritical($"Error while connected to IRC: {e.Code} - {e.Message}");
                    string errorParams = string.Join(' ', e.Parameters);
                    _log.LogCritical($"Parameters: {errorParams}");
                    break;
            }
        }

        private void BridgeBot_Error(object? sender, IrcErrorEventArgs e)
        {
            _log.LogError(e.Error, "Error in IRC module");
        }

        private void BridgeBot_ServerSupportedFeaturesReceived(object? sender, EventArgs e)
        {
            BridgeBot.ServerSupportedFeatures.TryGetValue("LINELEN", out string str);
            if (!string.IsNullOrEmpty(str))
            {
                if (Int32.TryParse(str, out int lineLength))
                {
                    this._maximumLineLength = lineLength;
                }

            }
        }

        public void Disconnect()
        {
            _log.LogInformation("Disconnecting from IRC");
            if (BridgeBot != null)
            {
                BridgeBot.Disconnect();
                BridgeBot.Dispose();
                BridgeBot = null;
                if (Bridge != null)
                {
                    Bridge.IrcIsConnected = false;
                }
            }
        }

        public void Reconnect()
        {
            try
            {
                _log.LogInformation("Reconnecting to IRC");
                // disconnect from IRC and dispose the old bridgebot
                Disconnect();
                // create a new bridgebot and connect to IRC
                Connect();
            }
            catch (Exception e)
            {
                string msg = e.Message;
            }
        }

        private void BridgeBot_Disconnected(object? sender, EventArgs e)
        {
            _log.LogInformation("IRC Disconnected");
            if (Bridge != null)
            {
                Bridge.IrcIsConnected = false;
            }
        }

        private void BridgeBot_Connected(object? sender, EventArgs e)
        {
            _log.LogInformation("Connected to IRC Server");
        }

        private void BridgeBot_Registered(object? sender, EventArgs e)
        {
            if (BridgeBot != null)
            {
                BridgeBot.LocalUser.JoinedChannel += LocalUser_JoinedChannel;
                BridgeBot.LocalUser.LeftChannel += LocalUser_LeftChannel;

                _log.LogInformation("Registered with IRC server, connecting to channels");
                var channels = _mappingConfiguration.Channels.Select(c => c.IrcChannelName).ToArray();
                BridgeBot?.Channels.Join(channels);

                ConnectMessage cm = new ConnectMessage();
                if (Bridge != null)
                {
                    Bridge.IrcIsConnected = true;
                }
                this.Bridge.Broadcast(this, cm);
            }
        }

        #region Bot User Events
        private void LocalUser_JoinedChannel(object? sender, IrcChannelEventArgs e)
        {
            _log.LogInformation($"Bot joined IRC channel {e.Channel.Name}");
            e.Channel.UsersListReceived += Channel_UsersListReceived;
            e.Channel.UserJoined += Channel_UserJoined;
            e.Channel.UserLeft += Channel_UserLeft;
            e.Channel.MessageReceived += Channel_MessageReceived;
            e.Channel.NoticeReceived += Channel_NoticeReceived;
            e.Channel.UserKicked += Channel_UserKicked;
            foreach (var mapping in _mappingConfiguration.Channels)
            {
                if (e.Channel.Name.ToLower().Trim() == mapping.IrcChannelName.ToLower().Trim())
                {
                    mapping.IrcChannel = e.Channel;
                }
            }
        }
        private void LocalUser_LeftChannel(object? sender, IrcChannelEventArgs e)
        {
            _log.LogInformation($"Bot left IRC channel {e.Channel.Name}");
            e.Channel.UsersListReceived -= Channel_UsersListReceived;
            e.Channel.UserJoined -= Channel_UserJoined;
            e.Channel.UserLeft -= Channel_UserLeft;
            e.Channel.MessageReceived -= Channel_MessageReceived;
            e.Channel.NoticeReceived -= Channel_NoticeReceived;
            e.Channel.UserKicked -= Channel_UserKicked;
        }
        #endregion

        #region Channel Events
        private void Channel_UsersListReceived(object? sender, EventArgs e)
        {
            if (sender is IrcChannel channel)
            {
                foreach (var user in channel.Users)
                {
                    if (!_trackedUsers.Contains(user.User))
                    {
                        _trackedUsers.Add(user.User);
                        user.User.Quit += User_Quit;
                        user.User.NickNameChanged += User_NickNameChanged;
                    }
                }
            }
        }

        private void User_NickNameChanged(object? sender, EventArgs e)
        {
            if (sender is IrcUser user)
            {
                // TODO: figure out how to find the old nickname since ircdotnet doesn't pass it (jerks)
            }
        }

        private void User_Quit(object? sender, IrcCommentEventArgs e)
        {
            if ((BridgeBot != null) && (sender is IrcUser user))
            {
                _log.LogDebug($"IRC User {user.NickName} quit: {e.Comment}");
                foreach (var channel in BridgeBot.Channels)
                {
                    bool found = false;
                    foreach (var channelUser in channel.Users)
                    {
                        if (channelUser.User == user)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        QuitMessage qm = new()
                        {
                            Channel = channel.Name,
                            User = user.NickName,
                            Text = e.Comment
                        };
                        this.Bridge?.Broadcast(this, qm);
                    }
                }
            }
        }

        private void Channel_UserJoined(object? sender, IrcChannelUserEventArgs e)
        {
            _log.LogDebug($"IRC User {e.ChannelUser.User.NickName} joined {e.ChannelUser.Channel.Name}: {e.Comment}");
            JoinMessage jm = new()
            {
                Channel = e.ChannelUser.Channel.Name,
                User = e.ChannelUser.User.NickName
            };
            this.Bridge?.Broadcast(this, jm);
        }
        private void Channel_UserLeft(object? sender, IrcChannelUserEventArgs e)
        {
            _log.LogDebug($"IRC User {e.ChannelUser.User.NickName} left {e.ChannelUser.Channel.Name}: {e.Comment}");
            PartMessage pm = new()
            {
                Channel = e.ChannelUser.Channel.Name,
                User = e.ChannelUser.User.NickName,
                Text = e.Comment
            };
            this.Bridge?.Broadcast(this, pm);
        }
        private void Channel_MessageReceived(object? sender, IrcMessageEventArgs e)
        {
            if (sender is IrcChannel channel)
            {
                if (e.Source is IrcUser user)
                {
                    _log.LogDebug($"IRC Message {user.NickName}:{channel.Name} - {e.Text}");
                    var mapping = _mappingConfiguration.Channels.FirstOrDefault(m => m.IrcChannel.Name == channel.Name);
                    if (mapping != null)
                    {
                        var ignore = mapping.IgnoreUsers.FirstOrDefault(u => u.ToLower().Trim() == user.NickName.ToLower().Trim());

                        if (string.IsNullOrWhiteSpace(ignore))
                        {
                            TextMessage tm = new()
                            {
                                Channel = channel.Name,
                                User = user.NickName,
                                Text = e.Text
                            };
                            this.Bridge?.Broadcast(this, tm);
                        }
                    }
                }
            }
        }
        private void Channel_NoticeReceived(object? sender, IrcMessageEventArgs e)
        {
            if (sender is IrcChannel channel)
            {
                if (e.Source is IrcUser user)
                {
                    _log.LogDebug($"IRC Notice {user.NickName}:{channel.Name} - {e.Text}");
                    var mapping = _mappingConfiguration.Channels.FirstOrDefault(m => m.IrcChannel.Name == channel.Name);
                    if (mapping != null)
                    {
                        var ignore = mapping.IgnoreUsers.FirstOrDefault(u => u.ToLower().Trim() == user.NickName.ToLower().Trim());

                        if (string.IsNullOrWhiteSpace(ignore))
                        {
                            NoticeMessage nm = new()
                            {
                                Channel = channel.Name,
                                User = user.NickName,
                                Text = e.Text
                            };
                            this.Bridge?.Broadcast(this, nm);
                        }
                    }
                }
            }
        }
        private void Channel_UserKicked(object? sender, IrcChannelUserEventArgs e)
        {
            _log.LogDebug($"IRC User {e.ChannelUser.User.NickName} kicked from {e.ChannelUser.Channel.Name}: {e.Comment}");
            KickMessage km = new()
            {
                Channel = e.ChannelUser.Channel.Name,
                User = e.ChannelUser.User.NickName,
                Text = e.Comment
            };
            this.Bridge?.Broadcast(this, km);
        }
        #endregion

        protected override void OnMessageBroadcast(TransportBase source, MessageBase message)
        {
            if (message is MapChannelMessage mapMessage)
            {
                bool alreadyJoined = false;
                foreach (var channel in BridgeBot.Channels)
                {
                    if (channel.Name.ToLower().Trim() == mapMessage.ChannelMapping.IrcChannelName.ToLower().Trim())
                    {
                        alreadyJoined = true;
                        mapMessage.ChannelMapping.IrcChannel = channel;
                    }
                }
                if (!alreadyJoined)
                {
                    BridgeBot.Channels.Join(mapMessage.ChannelMapping.IrcChannelName);
                }
            }
            if (message is TextMessage textMessage)
            {
                var mapping = _mappingConfiguration.Channels.FirstOrDefault(m => m.DiscordChannel.Name.ToLower().Trim() == textMessage.Channel.ToLower().Trim());

                if (mapping != null)
                {
                    // check and make sure we have an irc channel to send to
                    if (mapping.IrcChannel != null)
                    {
                        string nickPrefix = $"<{textMessage.User}> ";
                        int maxLength = _maximumLineLength - nickPrefix.Length;
                        string messageText = textMessage.Text.Trim();
                        messageText = MarkdownStringToIrcString(messageText);

                        if (messageText.Length <= maxLength)
                        {
                            // post the message
                            BridgeBot?.LocalUser.SendMessage(mapping.IrcChannel, nickPrefix + messageText);
                        }
                        else
                        {
                            var words = messageText.Split(' ');
                            List<string> list = new(words);
                            StringBuilder messageBuilder = new StringBuilder();
                            while (list.Count > 0)
                            {
                                var nextWord = list[0];
                                if (messageBuilder.Length + nextWord.Length < maxLength + 1)
                                {
                                    messageBuilder.Append(nextWord);
                                    messageBuilder.Append(" ");
                                    list.RemoveAt(0); // remove the word from the list
                                }
                                else if (messageBuilder.Length == 0)
                                {
                                    string word = nextWord.Substring(0, maxLength);
                                    BridgeBot?.LocalUser.SendMessage(mapping.IrcChannel, nickPrefix + word);
                                    list[0] = list[0].Substring(maxLength);
                                }
                                else
                                {
                                    // get the message
                                    messageText = messageBuilder.ToString().Trim();
                                    // send the message
                                    BridgeBot?.LocalUser.SendMessage(mapping.IrcChannel, nickPrefix + messageText);
                                    // clear the message builder
                                    messageBuilder.Length = 0;
                                }
                            }

                            if (messageBuilder.Length > 0)
                            {
                                // get the message
                                messageText = messageBuilder.ToString().Trim();
                                // send the message
                                BridgeBot?.LocalUser.SendMessage(mapping.IrcChannel, nickPrefix + messageText);
                            }
                        }
                    }

                }
                Console.WriteLine($"{textMessage.User}@Discord:{textMessage.Channel} - {textMessage.Text}");
            }
        }

        private string MarkdownStringToIrcString(string messageText)
        {
            return messageText; // TBD: convert markdown text into IRC text
            //var pipeline = new MarkdownPipelineBuilder()
            //    .UseCustomContainers()
            //    .Build();
            //StringWriter writer = new StringWriter();
            //var renderer = new Markdig.Renderers.Normalize.(writer);
            //pipeline.Setup(renderer);
            //renderer.ObjectRenderers.Insert(0, new container("test"));

            //var result = Markdown.ToPlainText(messageText);

            //MarkdownDocument document = Markdown.Parse(messageText, pipeline);
            //renderer.Render(document);
            //writer.Flush();
            //string markup = writer.ToString();

            //return markup;
        }

        //private string NodeToIrc(MarkdownObject block)
        //{
        //    var result = string.Empty;
        //    if (block is ContainerBlock container)
        //    {
        //        var descendents = block.Descendants().ToArray();
        //        var childBlocks = descendents.Select(NodeToIrc);
                
        //    }
        //    else if (block is LeafBlock leaf)
        //    {
                
        //    }
        //}

        internal override async Task Initialize(Bridge bridge)
        {
            await base.Initialize(bridge);
            if (ConfigurationValid())
            {
                Connect();
            }
            else
            {
                _log.LogCritical("Not connecting to IRC because of invalid configuration");
            }
        }

        private bool ConfigurationValid()
        {
            _log.LogInformation($"Validating connection for IRC:");
            _log.LogInformation($"  Nickname: {_configuration.Nickname}");
            _log.LogInformation($"  Alt: {_configuration.AlternateNickname}");
            _log.LogInformation($"  Real Name: {_configuration.RealName}");
            _log.LogInformation($"  Server: {_configuration.Server}");
            _log.LogInformation($"  Port: {_configuration.Port}");
            return
                !string.IsNullOrWhiteSpace(_configuration.Nickname) &&
                !string.IsNullOrWhiteSpace(_configuration.Username) &&
                !string.IsNullOrWhiteSpace(_configuration.RealName) &&
                !string.IsNullOrWhiteSpace(_configuration.Server) &&
                _configuration.Port > 0 &&
                _configuration.Port < 65535;
        }
    }
}
