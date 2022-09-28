# Code Architecture
## Code Overview
This project routes messages from an IRC server to a Discord server, and vice 
versa. It calls each of these connections a Transport. The part that 
coordinates those transports is called teh Bridge. There are implentations of
Transports for both IRC and Discord. Fundamentally, messages come in from 
either of these sources, are then sent back to the Bridge, which then
broadcasts them to all registered transports.

## Libraries
DiscordIrcBridge uses many libraries to implement its functionality.
### Logging
Logging is implemented using the [Serilog](https://serilog.net/) logging library.
### Discord
Discord functionality is implemented using the 
[Discord.Net](https://discordnet.dev/]) library, and commands are implemented
as SlashCommands via the InteractionFramework. You can find the commands under
the Transports/Discord/Modules directory.
### Irc
Irc functionality is implemented using the 
[ircdotnet](https://github.com/IrcDotNet/IrcDotNet) IRC library.

## Layout
Dependency injection is set up in Program.cs in MainAsync(). After the
ServiceProvider is built, it is used to create an instance of the 
[Bridge](../Bridge.cs] class. 

The Bridge class enables communication between IRC and Discord by 
orchestrating Message objects being broadcast between them. Bridge provides
a Broadcast() method that the Transports call, and provides a Broadcast event
for the Transports to subscribe to.

## Message Types
Discord and IRC both send messages of the TextMessage types. This is for any
channel user messaging.


| Message Type      | Discord | IRC | Description                                             |
| ------------      | ------- | --- | -----------                                             |
| MapChannelMessage | Yes     | No  | Sent when a discord channel is mapped to an IRC channel |
| AttachmentMessage | Yes     | No  | Sent when an attachment is sent to a Discord channel    |
| TextMessage       | Yes     | Yes | Sent when a text message is sent to Discord or IRC      |
| ConnectMessage    | No      | Yes | Sent when the IRC server connects                       |
| DisconnectMessage | No      | Yes | Sent when the IRC server disconnects                    |
| NicknameMessage   | No      | Yes | Sent when a user changes nickname on IRC                |
| JoinMessage       | No      | Yes | Sent when a user joins a channel on IRC                 |
| KickMessage       | No      | Yes | Sent when a user is kicked from a channel on IRC        |
| PartMessage       | No      | Yes | Sent when a user leaves a channel on IRC                |
| QuitMessage       | No      | Yes | Sent when a user quits IRC                              |
| NoticeMessage     | No      | Yes | Sent when a notice is sent to a channel on IRC          |

## Configuration
Configuration is persisted via the objects in the Configuration folder. A 
timer writes the configuration out once per minute using the 
ConfigurationHelper object. These configuration objects write out to json
files in the /data (or C:\data\) folder into irc.json, discord.json, 
statistics.json and mapping.json. These are further documented in the
[Configuration and Logging](ConfigurationAndLogging.md) section.