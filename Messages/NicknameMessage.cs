using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Messages
{
    internal class NicknameMessage : MessageBase
    {
        public string NewNickname { get; set; } = string.Empty;
    }
}
