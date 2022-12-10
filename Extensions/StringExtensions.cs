using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reactive.Joins;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace System
{
    public static class StringExtensions
    {
        public static string _urlPattern = @"https?:\/\/[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)";
        public static string _userPattern = @"<@(\d+)>";
        public static string _channelPattern = @"<#(\d+)>";
        private static List<(string Pattern, string ReplaceWith, bool ReplaceOnBothSides)> _patterns = new List<(string pattern, string surroundWith, bool onBothSides)>()
        {
            new ("__(.*)__", $"{(char)0x1F}", true), // underline
            new ("\\*\\*(.*)\\*\\*", $"{(char)0x02}", true), // bold
            new ("\\*(.*)\\*", $"{(char)0x1D}", true), // italic
            new ("_(.*)_", $"{(char)0x1D}", true) // italic
            //new ("\\~(.*)\\~", "~", true), // strikethrough
            //new ("\\`(.*)\\`", "`", true), // code block
            //new (@"^> (.*)$", "> ", true), // block quote
            //new (@"^>>> (.*)$", ">>> ", true) // block quote
        };
        
        public static string FromMarkdownToIrc(this string source, DiscordSocketClient client)
        {
            var links = Regex.Matches(source, _urlPattern);
            
            foreach(var pattern in _patterns)
            {
                if (Regex.IsMatch(source, pattern.Pattern))
                {
                    source = Regex.Replace(source, pattern.Pattern, delegate(Match m)
                    {
                        for(var l = 0; l < links.Count; l++)
                        {
                            var link = links[l];
                            if (link.Index <= m.Index && m.Index <= link.Index + link.Length)
                            {
                                return m.Value;
                            }
                        }
                        // tested not in a link
                        if (pattern.ReplaceOnBothSides)
                            return $"{pattern.ReplaceWith}{m.Groups[1].Value}{pattern.ReplaceWith}";
                        else
                            return $"{pattern.ReplaceWith}{m.Groups[1].Value}";
                    });
                }
            }

            source = Regex.Replace(source, _userPattern, delegate(Match m)
            {
                var stringUserId = m.Groups[1].Value;
                if (ulong.TryParse(stringUserId, out ulong userId))
                {
                    var user = client.GetUser(userId);
                    return "@" + user.Username;
                }
                return m.Value;
            });


            source = Regex.Replace(source, _channelPattern, delegate (Match m)
            {
                var stringChannelId = m.Groups[1].Value;
                if (ulong.TryParse(stringChannelId, out ulong channelId))
                {
                    var channel = client.GetChannel(channelId);
                    if (channel is SocketGuildChannel guildChannel)
                        return "#" + guildChannel.Name;
                }
                return m.Value;
            });
            return source;
        }
    }
}
