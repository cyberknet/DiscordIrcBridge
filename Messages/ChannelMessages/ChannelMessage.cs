using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Messages
{
    public class ChannelMessage : MessageBase
    {
        public string User { get; set; }
        public string Channel { get; set; }
    }
}
