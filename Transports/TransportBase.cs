using DiscordIrcBridge.Configuration;
using DiscordIrcBridge.Messages;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Transports
{
    public abstract class TransportBase
    {
        public Bridge? Bridge { get; private set; }
        public TransportBase()
        {
        }

        private void Bridge_MessageBroadcast(object? sender, MessageEventArgs e)
        {
            if (e.SourceTransport != this)
            {
                OnMessageBroadcast(e.SourceTransport, e.Message);
            }
        }

        protected abstract void OnMessageBroadcast(TransportBase source, MessageBase message);

        internal virtual async Task Initialize(Bridge bridge)
        {
            Bridge = bridge;
            Bridge.MessageBroadcast += Bridge_MessageBroadcast; ;
            return;
        }
    }
}
