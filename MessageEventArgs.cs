using DiscordIrcBridge.Messages;
using DiscordIrcBridge.Transports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge
{
    public class MessageEventArgs
    {
        public MessageBase Message { get; set; }
        public TransportBase SourceTransport { get; set; }

        public MessageEventArgs(TransportBase sourceTransport, MessageBase message)
        {
            Message = message;
            SourceTransport = sourceTransport;
        }
    }
}
