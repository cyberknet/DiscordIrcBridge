using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Messages
{
    internal class PartMessage : ChannelMessage
    {
        public string Text { get; set; } = string.Empty;
    }
}
