using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Configuration
{
    public class DiscordConfiguration
    {
        public char CommandPrefix { get; set; } = '!';
        public ulong GuildId { get; set; }
        public string Token { get; set; }
        public ulong? DebugChannelWebhookId { get; set; }
        public string? DebugChannelWebhookToken { get; set; }
    }
}
