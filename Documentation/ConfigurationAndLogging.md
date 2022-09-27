# Configuration And Logging
The bot will store its configuration files in /data or C:\data (depending on 
UNIX or Windows)
## Secondary Configuration
```
/data/settings.json
```
Allows you to override any of the default appsettings.json values. This file is
not required. If this file is provided, the following sample shows the default 
Serilog configuration. It is not recommended to create or modify this file 
unless you have development experience.

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Discord" ],
    "MinimalLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:o} {SourceContext} [{Level:u3}] {Message}{NewLine}{Exception}",
          "theme": "Serilog.Sinks.SystemConsole.Themes.SystemConsoleTheme::Grayscale, Serilog.Sinks.Console"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "/data/discordircbridge.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ],
    "Enrich": [
      "FromLogContext"
    ]
  }
}

```
## Discord Configuration
```
/data/discord.json
```
Stores information about its Discord connection including Server/Guild Id, and 
Bot Token.  

| Property                 | Type   | Description                                                                 |
| --------                 | ----   | -----------                                                                 |
| CommandPrefix            | String | Command prefix for text-based commands. Single Character Only. (Future use) |
| GuildId                  | UInt32 | Server/Guild Id you want the bot to join by default                         |
| Token                    | string | Bot token as recorded from Step 10 in [How to Create a Discord Bot](HowToCreateADiscordBot.md#creating-a-discord-bot) |
| DebugChannelWebhookId    | UInt32 | Reserved for future use                                                     |
| DebugChannelWebhookToken | string | Reserved for future use                                                     |

**Sample**
```json
{
  "CommandPrefix": "!",
  "GuildId": 123456789012345678,
  "Token": "MTayNDMwODQyMDEwMDIONzU3Mg.GXhazp.3GVasW9dD4ENnTX57oaNPjrNG3eeivCOelaiFU",
  "DebugChannelWebhookId": null,
  "DebugChannelWebhookToken": null
}
```

## IRC Configuration
```
/data/irc.json
```
Stores information about the IRC connection configuration  

| Property                 | Type   | Description                                                                 |
| --------                 | ----   | -----------                                                                 |
| Nickname                 | String | Primary nickname used to connect to IRC                                     |
| AlternateNickName        | String | Secondary nickname used in IRC when primary is already in use               |
| Username                 | String | Username to use when connecting to IRC                                      |
| RealName                 | String | Real name to display when connected to IRC                                  |
| Password                 | String | Password to use when connecting to IRC (future enhancement)                 |
| Server                   | String | Fully qualified domain name or IP Address of IRC Server                     |
| Port                     | Int32  | Port to connect to IRC Server                                               |
| ConnectionTimeout        | Int32  | Timeout in ms for connecting to the IRC Server                              |
| ClientQuitTimeout        | Int32  | Timeout in ms to wait for response to a QUIT command before disconnecting.  |
| FloodMaxMessageBurst     | Int32  | Number of messages to burst sending before rate limiting                    |
| FloodCounterPeriod       | Int32  | Duration in ms for rate limiting to check for message bursting              |



```json
{
  "Nickname": "myBotNick",
  "AlternateNickname": "myBotNick2",
  "Username": "myBotUser",
  "RealName": "myBot Real Name",
  "Password": "",
  "Server": "irc.libera.chat",
  "Port": 6667,
  "ConnectionTimeout": 30000,
  "ClientQuitTimeout": 0,
  "QuitMessage": "DiscordIRCBridge Bot",
  "FloodMaxMessageBurst": 4,
  "floodCounterPeriod": 2000
}
```
## User and Channel Mapping
```
/data/mapping.json
```
Stores information about which discord channels are mapped to which IRC 
channels, as well as what Discord usernames are mapped to a given IRC username.

| Property                 | Type   | Description                                                                 |
| --------                 | ----   | -----------                                                                 |
| Channels                 | Array  | List of Discord channels mapped to IRC channels                             |
| Users                    | Array  | List of Discord users mapped to IRC users                                   |

**Channel Mapping Object**

| Property                 | Type   | Description                                                                 |
| --------                 | ----   | -----------                                                                 |
| DiscordChannelId         | UInt32 | Id identifying Discord Channel                                              |
| IrcChannelName           | String | Name of the associated channel on IRC                                       |
| DiscordWebHook           | String | Url for the Discord Channel Webhook                                         |
| IgnoreUsers              | Array  | String array of IRC nicknames to ignore                                     |

**User Mapping Object**

| Property                 | Type   | Description                                                                 |
| --------                 | ----   | -----------                                                                 |
| DiscordUserId            | UInt32 | Id identifying Discord User (NOT name+discriminator)                        |
| IrcNickname              | String | Nickname for the user on IRC                                                |

```json
{
  "Channels": [
    {
      "DiscordChannelId": 123456789012345678,
      "IrcChannelName": "##ircChannel",
      "DiscordWebHook": "https://discord.com/api/webhooks/123456789012345678/abc12De_fG_HijkLmnoPqRs3TU4vwXyZABcdEfg5HIJklMNopqrSTuVwx6Yz78AbCd9",
      "IgnoreUsers": [
        "otherIrcBot"
      ]
    }
  ],
  "Users": [
    {
      "DiscordUserId": 456789012345678901,
      "IrcNickname": "IrcUserNick"
    }
  ]
}
```
## Statistics
```
/data/statistics.json
```
Keeps some running statistics about the bot including uptime and messages
processed. It is not recommended to modify this file.
```json
{
  "ChannelMessages": {
    "123456789012345678": 328,
    "987654321098765432": 167
  },
  "PreviousUptime": "23:13:00",
  "LastStartedAt": "2022-09-26T22:35:28.2963794-05:00",
  "CommandsProcessed": 54,
  "IrcDisconnections": 1
}
```
## Log Files
```
/data/discordircbridge[yyyymmdd].log
```
The bot will keep log files with activity and troubleshooting information. 
These log files roll over to a new file every day. The bot will keep at most
7 days worth of log files.

***WARNING: DO NOT POST YOUR LOG FILES ONLINE***  

The Discord bot token for your bot is present in your log files. This would
allow anyone to configure their discord bot with your token. At best, this
would result in spam to your server. At worst (depending on privileges given)
it could allow someone to take over your guild completely.  

***WARNING: DO NOT POST YOUR LOG FILES ONLINE***
