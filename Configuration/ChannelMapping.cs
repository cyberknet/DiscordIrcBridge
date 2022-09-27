using Discord.Webhook;
using Discord.WebSocket;
using IrcDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Configuration
{
    public class ChannelMapping
    {
        public ulong DiscordChannelId { get; set; }
        public string IrcChannelName { get; set; }
        public string DiscordWebHook { get; set; }
        public List<string> IgnoreUsers { get; set; } = new List<string>();

        [JsonIgnore]
        public DiscordWebhookClient DiscordWebHookClient { get; set; }
        [JsonIgnore]
        public SocketGuildChannel? DiscordChannel { get; internal set; }
        [JsonIgnore]
        public IrcChannel IrcChannel { get; set; }
    }
}
