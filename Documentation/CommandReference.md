# Command Reference
## Bridge Commands
### /bridge list
Lists the users preesnt on the IRC side of the list. Shows any mappings for the
users on the IRC side.  
**Example**  
```
/bridge list
```

### Bridge Reconnect Command
Reconnects to the IRC server following a disconnection. If the IRC server is
currently connected, the connection will be disconnected first.  
**Example**  
```
/bridge reconnect
```

### Bridge Setup Command
After running the command, the bot will save the IRC connection settings and 
attempt to connect to IRC.  
**Parameters:**  

| Name        | Type      | Descrption                                                   |
| ----------- | ----      | ----------                                                   |
| serverName  | text      | fully qualified domain name or IP address for the IRC server |
| port        | number    | the IRC server port                                          |
| nickname    | text      | Nickname to appear as on IRC                                 |
| altNickname | text      | Alternate nickname for IRC if the primary nickname is taken  |
| username    | text      | Username to display                                          |
| realName    | text      | Value to display in the real name section of IRC             |

**Example**  
```
/bridge setup irc.libera.chat 6667 myBotNick myBotNick2 myBotUser "myBot Real Name"
```

### Bridge Settings
Returns the current settings for the IRC connection.  
**Example**  
```
/bridge settings
```

### Bridge Status
Shows whether the IRC bridge is currently connected or disconnected.  
**Example**  
```
/bridge status
```

## Channel Commands
### Channel Ignore
**Required Role**
- Bridge Admin  
Ignores an IRC user and prevents their text from coming over to Discord. This 
can be useful to prevent bot feedback.  
**Parameters:**  

| Name           | Type      | Descrption                                                   |
| -----------    | ----      | ----------                                                   |
| channel        | channel   | Tagged discord channel that is mapped to an IRC channel      |
| ignoreNickname | text      | nickname to be ignored on IRC                                |

**Example**  
```
/channel ignore #discordChannel otherIrcBot
```
### Channel Map
Maps a Discord channel to an IRC channel.  
**Required Role**  
- Bridge Admin  

**Parameters**  

| Name           | Type      | Descrption                                                   |
| -----------    | ----      | ----------                                                   |
| channel        | channel   | Tagged discord channel to map to an IRC channel              |
| ircChannel     | text      | channel name on IRC                                          |
| webhook        | text      | (optional) webhook URL (See [Webhooks](../README.md#webhooks))  |

**Example**  
```
/channel map #discordChannel ##ircChannel  
/channel map #discordChannel ##ircChannel https://discord.com/api/webhooks/123456789012345678/abc12De_fG_HijkLmnoPqRs3TU4vwXyZABcdEfg5HIJklMNopqrSTuVwx6Yz78AbCd9
```
### Channel Unmap
Removes a mapping for a Discord and IRC channel pair.  
**Required Role**  
- Bridge Admin  

**Parameters**  

| Name           | Type      | Descrption                                                   |
| -----------    | ----      | ----------                                                   |
| channel        | channel   | Tagged discord channel to unmap from an IRC channel          |
| ircChannel     | text      | channel name on IRC                                          |

**Example**
```/channel unmap #discordChannel ##ircChannel```

### Channel Setwebhook
Allows you to set a webhook for a channel mapping in the case that the webhook
was not available when the channel was initially mapped.  
**Required Role**  
- Bridge Admin  

**Parameters**

| Name           | Type      | Descrption                                                   |
| -----------    | ----      | ----------                                                   |
| channel        | channel   | Tagged discord channel to map to an IRC channel              |
| webhook        | text      | webhook URL (See [Webhooks](../README.md#webhooks))             |

**Example**  
```
/channel setwebhook #discordChannel https://discord.com/api/webhooks/123456789012345678/abc12De_fG_HijkLmnoPqRs3TU4vwXyZABcdEfg5HIJklMNopqrSTuVwx6Yz78AbCd9
```

## Info Commands
### Info Debugchannel
Sets a channel for bot logs to be sent to.  This setting cannot take effect until the bot is restarted.  
**Required Role**  
- Bridge Admin  

**Parameters**

| Name           | Type      | Descrption                                                   |
| -----------    | ----      | ----------                                                   |
| channel        | channel   | Tagged discord channel to map to an IRC channel              |


**Example**  
```
/info debugchannel #discordChannel
```

### Info Statistics
Shows statistics on when the bot was started, uptime, messages processed, 
commands processed, and irc server disconnections.

**Example**  
```
/info statistics
```


## User Commands
### User Discord
Shows any IRC users that are mapped to a Discord user.  

**Parameters**

| Name           | Type      | Descrption                                                   |
| -----------    | ----      | ----------                                                   |
| user           | User      | Tagged discord user to locate IRC nickname mappings          |

**Example**  
```
/user discord @discordUser
```

### User Irc
Shows any Discord users that are mapped to an IRC nickname.  

**Parameters**

| Name           | Type      | Descrption                                                   |
| -----------    | ----      | ----------                                                   |
| ircNickname    | text      | Nickname on IRC to locate Discord user mappings              |


**Example**  
```
/user irc ircNickname
```

### User Map
Maps a discord user to an IRC nickname.
**Required Role**  
- Bridge Admin  

**Parameters**

| Name           | Type      | Descrption                                                   |
| -----------    | ----      | ----------                                                   |
| channel        | channel   | Tagged discord channel to map to an IRC channel              |
| ircNickname    | text      | IRC Nickname to map the discord user to                      |


**Example**  
```
/user map @discordUser ircNickname
```
