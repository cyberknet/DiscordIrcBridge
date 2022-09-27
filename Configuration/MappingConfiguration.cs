using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Configuration
{
    public class MappingConfiguration
    {
        public List<ChannelMapping> Channels { get; set; } = new();
        public List<UserMapping> Users { get; set; } = new();
    }
}
