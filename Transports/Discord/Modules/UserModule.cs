using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordIrcBridge.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Discord.Color;

namespace DiscordIrcBridge.Transports.Discord.Modules
{
    // interaction modules must be public and inherit from an IInteractionModuleBase
    [Group("user", "Commands related to the user functionality.")]
    public class UserModule : InteractionModuleBase<SocketInteractionContext>
    {
        // dependencies can be accessed through Property injection, public properties with public setters will be set by the service provider
        public InteractionService Commands { get; set; }

        private DiscordTransport _handler;
        private DiscordSocketClient _client;
        private MappingConfiguration _mappingConfiguration;

        public UserModule(DiscordTransport discordTransport, DiscordSocketClient client, MappingConfiguration mappingConfiguration)
        {
            _handler = discordTransport;
            _client = client;
            _mappingConfiguration = mappingConfiguration;
        }


        [SlashCommand("discord", "Displays all IRC nicknames mapped to a discord user id.")]
        public async Task Discord(IUser user)
        {
            string[] ircNicknames = _mappingConfiguration.Users
                .Where(u => u.DiscordUserId == user.Id)
                .Select(u => u.IrcNickname)
                .ToArray();


            if (ircNicknames.Length == 0)
            {
                await RespondAsync($"<@{user.Id}> has no known IRC aliases");
            }
            else
            { 
                string formattedNames = ircNicknames[ircNicknames.Length - 1];
                if (ircNicknames.Length == 2)
                {
                    formattedNames = String.Join(" and ", ircNicknames);
                }
                else if (ircNicknames.Length > 2)
                {
                    formattedNames =
                        String.Join(", ", ircNicknames.Take(ircNicknames.Length - 1)) +
                        ", and " + ircNicknames[ircNicknames.Length - 1];
                }

                var embedBuilder = new EmbedBuilder()
                    .WithTitle($"IRC Aliases for Discord User {user.Username}")
                    .WithDescription($"{formattedNames}")
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();
                await RespondAsync(embed: embedBuilder.Build());
            }
        }

        [SlashCommand("irc", "Displays all discord users mapped to an IRC nickname.")]
        public async Task Irc(string nickname)
        {
            var cmp = nickname.ToLower().Trim();
            ulong[] discordIds = _mappingConfiguration.Users
                .Where(u => u.IrcNickname.ToLower().Trim() == cmp)
                .Select(u => u.DiscordUserId)
                .ToArray();


            if (discordIds.Length == 0)
            {
                await RespondAsync($"IRC user {nickname} has no known Discord aliases");
            }
            else
            {
                var discordNames = discordIds.Select(
                    (id) =>
                    {
                        var discordUser = _client.GetUser(id);
                        return $"<@{discordUser.Id}>";
                    }).ToArray();
                string formattedNames = discordNames[discordNames.Length - 1];
                if (discordNames.Length == 2)
                {
                    formattedNames = String.Join(" and ", discordNames);
                }
                else if (discordNames.Length > 2)
                {
                    formattedNames =
                        String.Join(", ", discordNames.Take(discordNames.Length - 1)) +
                        ", and " + discordNames[discordNames.Length - 1];
                }

                var embedBuilder = new EmbedBuilder()
                    .WithTitle($"Discord Aliases for IRC User {nickname}")
                    .WithDescription($"{formattedNames}")
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();
                await RespondAsync(embed: embedBuilder.Build());
            }
        }
        [SlashCommand("map", "Maps a discord username to an IRC username.")]
        [RequireRole("bridgeadmin")]
        public async Task Map(IUser user, string ircNickname)
        {
            bool added = false;
            var exists = _mappingConfiguration.Users.Count(m => m.DiscordUserId == user.Id && ircNickname.ToLower().Trim() == m.IrcNickname.ToLower().Trim()) > 0;
            if (!exists)
            {
                _mappingConfiguration.Users.Add(new UserMapping
                {
                    IrcNickname = ircNickname,
                    DiscordUserId = user.Id
                });
                ConfigurationHelper.SaveMappingConfiguration(_mappingConfiguration);
                await RespondAsync($"Discord user <@{user.Id}> is now mapped to IRC user {ircNickname}");
            }
            else
            {
                await RespondAsync($"Discord user <@{user.Id}> is already mapped to IRC user {ircNickname}");
            }
        }


    }
}
