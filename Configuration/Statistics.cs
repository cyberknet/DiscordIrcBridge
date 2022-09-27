using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Configuration
{
    public class Statistics
    {
        public Dictionary<ulong, int> ChannelMessages { get; set; } = new ();
        public TimeSpan PreviousUptime { get; set; }
        public DateTime LastStartedAt { get; set; }
        public int CommandsProcessed { get; set; }
        public int IrcDisconnections { get; set; }
    }
}
