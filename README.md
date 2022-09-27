# DiscordIrcBridge
Creates a bridge between Discord and IRC, moving messages back and forth 
between them.

## Installation
Installation consists of creating a bot on the Discord website, running the bot
through docker, and configuring the bot after it is up and running. Depending
on your experience with Discord, Bots, and/or Docker - any of these may be an
easy task for a seasoned pro-user, or daunting as a new experience.

To assist, the following guides have been created broken out by task:
1. [How to create a discord bot](Documentation/HowToCreateADiscordBot.md)
2. [How to run the bot](Documentation/HowToRunTheBot.md)
3. [How to configure the bot](Documentation/HowToConfigureTheBot.md)

## Webhooks
The bot supports using
[Discord Webhooks](https://support.discord.com/hc/en-us/articles/228383668-Intro-to-Webhooks)
to post messages so that they appear to be posted by a mapped discord user for 
each IRC user.  

The default display for an unmapped user has the bot send the text to discord,
prefixed with the user's nickname:  
![Unmapped user with prefixed display](Images/DiscordUnmappedText.png)

When a webhook is specified for the channel, messages sent by mapped users will
appear on Discord as having been sent by their mapped user id. The message
will be marked with a [bot] tag to differentiate between messages the user
sent on Discord, and messages posted by the bot:  
![Mapped user message](Images/DiscordMappedText.png)

Note that currently the bot only identifies users by nickname, and does not
take host mask into account. This can lead to messages appearing on Discord
that the "real" IRC user did not send. Using hostmask to consider these is
planned for the future.

## Commands
[Bot Command Reference](Documentation/CommandReference.md)

## Configuration and Logging
[Configuration And Logging](Documentation/ConfigurationAndLogging.md)

## Contribution

## Feedback
