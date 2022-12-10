using DiscordIrcBridge.Configuration;
using DiscordIrcBridge.Messages;
using DiscordIrcBridge.Transports;
using DiscordIrcBridge.Transports.Discord;
using DiscordIrcBridge.Transports.Irc;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceProvider _serviceProvider;
        private readonly Statistics _statistics;
        private IrcTransport _ircTransport;
        private DiscordTransport _discordTransport;

        private System.Timers.Timer _autoSaveTimer;

        public bool IrcIsConnected { get; set;  }

        public Bridge(IrcConfiguration ircConfiguration, DiscordConfiguration discordConfiguration, MappingConfiguration mappingConfiguration, Statistics statistics, IServiceProvider serviceProvider)
        {
            _ircConfiguration = ircConfiguration;
            _discordConfiguration = discordConfiguration;
            _mappingConfiguration = mappingConfiguration;
            _statistics = statistics;
            _serviceProvider = serviceProvider;

            _discordTransport = serviceProvider.GetRequiredService<DiscordTransport>();
            _ircTransport = serviceProvider.GetRequiredService<IrcTransport>();
            _ircTransport.Disconnected += IrcTransport_Disconnected;
            _transports.Add(_discordTransport);
            _transports.Add(_ircTransport);

            _autoSaveTimer = new System.Timers.Timer();
            _autoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
            _autoSaveTimer.Interval = TimeSpan.FromSeconds(60).TotalMilliseconds;
            _autoSaveTimer.Enabled = true;
        }

        private void IrcTransport_Disconnected(object? sender, EventArgs e)
        {
            _ircTransport.Dispose();
            _transports.Remove(_ircTransport);
            _ircTransport = _serviceProvider.GetRequiredService<IrcTransport>();
            _transports.Add(_ircTransport);
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
