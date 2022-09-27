using IrcDotNet;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DiscordIrcBridge.Transports.Irc.Formatting
{
    public class IrcFormatting
    {

        private const string C_REGEX = @"\x03(\d\d?)(,(\d\d?))?";
        


        private static Dictionary<char, string> Keys = new ()
        {
            [IrcBlock.FORMAT_BOLD] = "bold",
            [IrcBlock.FORMAT_ITALIC] = "italic",
            [IrcBlock.FORMAT_UNDERLINE] = "underline"
        };


        public List<IrcBlock> Parse(string text)
        {
            var result = new List<IrcBlock>();
            IrcBlock? current = new IrcBlock(null);
            IrcBlock? prev = null;
            MatchCollection? colorMatches;
            var colorRegex = new Regex(C_REGEX);
            var startIndex = 0;

            // find all color matches
            colorMatches = colorRegex.Matches(text);

            // Append a resetter to simplify code a bit
            text += IrcBlock.FORMAT_RESET;

            for(var i = 0; i < text.Length; i++)
            {
                current.Text = text.Substring(startIndex, i - startIndex);
                var ch = text[i];
                
                switch(ch)
                {
                    // bold, italic, underline
                    case IrcBlock.FORMAT_BOLD:
                    case IrcBlock.FORMAT_ITALIC:
                    case IrcBlock.FORMAT_UNDERLINE:
                        prev = current;
                        current = new IrcBlock(prev);
                        
                        // toggle style
                        current.Styles[ch] = !prev.Styles[ch];
                        startIndex = i + 1;
                        break;
                    
                    // color
                    case IrcBlock.FORMAT_COLOR:
                        prev = current;
                        current = new IrcBlock(prev);
                        var color = GetColorMatch(colorMatches, i);
                        if (color != null)
                        {
                            if (Int32.TryParse(color.Groups[1].Value, out int primaryColor))
                            {
                                int highlightColor = prev.Highlight;
                                if (Int32.TryParse(color.Groups[3].Value, out int secondaryColor))
                                {
                                    highlightColor = secondaryColor;
                                }
                                current.Color = primaryColor;
                                current.Highlight = highlightColor;
                                
                                startIndex = i + color.Groups[0].Length;
                                i = startIndex - 1;
                            }
                        }
                        else
                        {
                            current.Color = -1; // no color
                            current.Highlight = -1; // no color
                            startIndex = i + 1;
                        }
                        break;

                    // reverse
                    case IrcBlock.FORMAT_REVERSE:
                        prev = current;
                        current = new IrcBlock(prev);
                        if (prev.Color != -1)
                        {
                            current.Color = prev.Highlight;
                            current.Highlight = prev.Color;
                            if (current.Color == -1)
                            {
                                current.Color = 0;
                            }
                        }
                        current.Reverse = !prev.Reverse;
                        startIndex = i + 1;
                        break;

                    // reset
                    case IrcBlock.FORMAT_RESET:
                        prev = current;
                        current = new IrcBlock(null);
                        startIndex = i + 1;
                        break;
                    default: 
                        continue;
                }
                result.Add(prev);
            } // for
            result.Add(current);

            return result;
        }

        public string PlainText(List<IrcBlock> blocks)
        {
            var textParts = blocks.Select(b => b.Text);
            return string.Concat(textParts);
        }
        public List<IrcBlock> RemoveStyle(List<IrcBlock> blocks)
        {
            for(var i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                block.Bold = block.Italic = block.Underline = false;
            }
            return blocks;
        }
        public List<IrcBlock> RemoveColor(List<IrcBlock> blocks)
        {
            for( var i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                block.Color = block.Highlight = -1;
            }
            return blocks;
        }

        public List<IrcBlock> Compress(List<IrcBlock> blocks)
        {
            if (blocks.Count <= 1)
                return blocks;

            var last = blocks[0];
            for(var i = 1; i < blocks.Count; i++)
            {
                var block = blocks[i];

                if (block.Bold == last.Bold &&
                    block.Italic == last.Italic &&
                    block.Underline == last.Underline &&
                    block.Color == last.Color &&
                    block.Highlight == last.Highlight)
                {
                    last.Text += block.Text;
                    blocks.RemoveAt(i--);
                }
                else
                    last = block;
            }
            return blocks;
        }


        private Match? GetColorMatch(MatchCollection matches, int index)
        {
            for (var i = 0; i < matches.Count; ++i)
            {
                var match = matches[i];

                if (index == match.Index)
                    return match;
                else if (match.Index > index)
                    return null;
            }

            return null;
        }
    }
}
