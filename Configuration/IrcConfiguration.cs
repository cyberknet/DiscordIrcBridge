using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Configuration
{
    public class IrcConfiguration
    {
        public string Nickname { get; set; } = "";
        public string AlternateNickname { get; set; } = "";
        public string Username { get; set; } = "";
        public string RealName { get; set; } = "";
        public string Password { get; set; } = "";
        public string Server { get; set; } = "";
        public int Port { get; set; } = 6667;
        public int ConnectionTimeout { get; set; } = 30000;
        public int ClientQuitTimeout { get; set; }
        public string QuitMessage { get; set; } = "DiscordIRCBridge Bot";

        public int FloodMaxMessageBurst { get; set; }
        public int floodCounterPeriod { get; set; }
    }
}
