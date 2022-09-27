using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Messages
{
    public class TextMessage : ChannelMessage
    {
        public string Text { get; set; }
    }
}
