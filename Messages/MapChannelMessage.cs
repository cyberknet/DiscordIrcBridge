using DiscordIrcBridge.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Messages
{
    public class MapChannelMessage : MessageBase
    {
        public ChannelMapping ChannelMapping { get; set; }
    }
}
