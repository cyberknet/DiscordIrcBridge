using IrcDotNet.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordIrcBridge.Transports.Irc.Formatting
{
    public class IrcBlock
    {
        internal const char FORMAT_BOLD = '\x02';
        internal const char FORMAT_ITALIC = '\x1d';
        internal const char FORMAT_UNDERLINE = '\x1f';
        internal const char FORMAT_COLOR = '\x03';
        internal const char FORMAT_REVERSE = '\x16';
        internal const char FORMAT_RESET = '\x0f';

        private readonly IrcBlock? _previousBlock;

        public bool Reverse { get; set; } = false;
        public int Color { get; set; } = -1;
        public int Highlight { get; set; } = -1;
        public string Text { get; set; } = string.Empty;

        public bool Bold { get => Styles[FORMAT_BOLD]; set => Styles[FORMAT_BOLD] = value; }
        public bool Italic { get => Styles[FORMAT_ITALIC]; set => Styles[FORMAT_ITALIC] = value; }
        public bool Underline { get => Styles[FORMAT_UNDERLINE]; set => Styles[FORMAT_UNDERLINE] = value; }

        public Dictionary<char, bool> Styles { get; set; } = new()
        {
            [FORMAT_BOLD] = false,
            [FORMAT_ITALIC] = false,
            [FORMAT_UNDERLINE] = false
        };
        public IrcBlock(IrcBlock? previousBlock)
        {
            this._previousBlock = previousBlock;
            if (previousBlock != null)
            {
                Styles.Clear();
                foreach (var style in previousBlock.Styles)
                    Styles.Add(style.Key, style.Value);
            }

        }
    }
}
