using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Configuration
{
    public class UserMapping
    {
        public ulong DiscordUserId { get; set; }
        public string IrcNickname { get; set; }
    }
}
