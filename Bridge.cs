using DiscordIrcBridge.Configuration;
using DiscordIrcBridge.Messages;
using DiscordIrcBridge.Transports;
using DiscordIrcBridge.Transports.Discord;
using DiscordIrcBridge.Transports.Irc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge
{
    public class Bridge
    {
        public event EventHandler<MessageEventArgs>? MessageBroadcast;

        private List<TransportBase> _transports = new List<TransportBase>();

        private readonly MappingConfiguration _mappingConfiguration;
        private readonly DiscordConfiguration _discordConfiguration;
        private readonly IrcConfiguration _ircConfiguration;
        private readonly Statistics _statistics;

        private System.Timers.Timer _autoSaveTimer;

        public bool IrcIsConnected { get; set;  }

        public Bridge(DiscordTransport discordTransport, IrcTransport ircTransport, IrcConfiguration ircConfiguration, DiscordConfiguration discordConfiguration, MappingConfiguration mappingConfiguration, Statistics statistics)
        {
            _transports.Add(discordTransport);
            _transports.Add(ircTransport);
            _ircConfiguration = ircConfiguration;
            _discordConfiguration = discordConfiguration;
            _mappingConfiguration = mappingConfiguration;
            _statistics = statistics;

            _autoSaveTimer = new System.Timers.Timer();
            _autoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
            _autoSaveTimer.Interval = TimeSpan.FromSeconds(60).TotalMilliseconds;
            _autoSaveTimer.Enabled = true;
        }

        ~Bridge()
        {
            SaveConfigurations();
        }

        private void SaveConfigurations()
        {
            ConfigurationHelper.SaveMappingConfiguration(_mappingConfiguration);
            ConfigurationHelper.SaveDiscordConfiguration(_discordConfiguration);
            ConfigurationHelper.SaveIrcConfiguration(_ircConfiguration);
            ConfigurationHelper.SaveStatistics(_statistics, false);
        }

        [DebuggerStepThrough()]
        private void AutoSaveTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            SaveConfigurations();
        }

        public void Broadcast(TransportBase source, MessageBase message)
        {
            OnBroadcast(source, message);
        }

        public async void Initialize()
        {
            foreach(var transport in _transports)
            {
                await transport.Initialize(this);
            }
        }

        public void ReconnectIrc()
        {
            foreach(var transport in _transports)
            {
                if (transport is IrcTransport irc)
                {
                    irc.Reconnect();
                }
            }
        }

        protected virtual void OnBroadcast(TransportBase source, MessageBase message)
        {
            MessageBroadcast?.Invoke(this, new MessageEventArgs(source, message));
        }
    }
}
