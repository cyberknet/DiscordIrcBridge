﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Messages
{
    internal class KickMessage : ChannelMessage
    {
        public string Text { get; set; } = string.Empty;
    }
}