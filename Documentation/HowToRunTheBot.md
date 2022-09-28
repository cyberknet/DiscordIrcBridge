# How To Run The Bot
By now, you have created the bot on Discord, and invited it to the server.
However, the bot is not actually running! To run the bot, you will need to run
the code. You can do this via one of multiple methods:
## Running in Docker-Compose
The easiest way to run the bot on docker is with Docker-Compose. Installing
and configuring docker-compose is outside the scope of these instructions. You
can find help on that topic at the [docker-compose page](https://docs.docker.com/compose/).
1. You will need to select either docker-compose-amd64.yml or
   docker-compose-arm64.yml. If you are running a Raspberry Pi 4, choose arm64.
   If you are running on a Windows or Linux machine with an Intel-compatible CPU,
   choose amd64.
1. Copy the docker-compose-[arch].yml from the Docker directory to a directory locally.
1. Copy the .env file from the Docker directory to the same directory as
   docker-compose.yml.
1. Edit the .env file and add in the Bot Token that you copied in Step 10 of 
   Creating a Discord Bot above. Your final file will look something like:
   ```
   DISCORD_GUILDID=293879474672106934
   DISCORD_TOKEN=MTayNDMwODQyMDEwMDIONzU3Mg.GXhazp.3GVasW9dD4ENnTX57oaNPjrNG3eeivCOelaiFU
   DISCORD_COMMANDPREFIX=!
   ```
   a. NOTE: the Guild ID and Token above are for example purposes only. You 
   need to substitute in your own guild id and bot token.
1. From the directory that the docker-compose.yml and .env file are in, run
   "docker-compose up"
1. The bot should now be running and listening in your server.
## Running in Docker without Docker-Compose
The bot requires one volume mounted in /data and you will need to provide three
environment variables the first time you run it so that it knows what guild to
initialize and what token to authorize with. 

You will need to choose an appropriate tag for the architecture of your CPU.
If you are running on a Rapsberry Pi, choose arm64. If you are running on an
Intel-compatible 64bit CPU, choose amd64. You can use a command similar to
the example below after substituting [tag] for amd64 or arm64:
  ```
  docker run -d -t- i \
  -e DISCORD_GUILDID=293879474672106934 \
  -e DISCORD_TOKEN=MTayNDMwODQyMDEwMDIONzU3Mg.GXhazp.3GVasW9dD4ENnTX57oaNPjrNG3eeivCOelaiFU \
  -e DISCORD_COMMANDPREFIX=! \
  -v data:/data
  -name discordircbridge sblomfield/discordircbridge:[tag]

  ```
  a. NOTE: the Guild ID and Token above are for example purposes only. You need
     to substitute in your own guild id and bot token.
## Running from an executable on your PC
No executable files are published for installation, however the source code is 
available and you are encouraged to clone the repository and build the code 
yourself. The code requires .NET 7.0 and Visual Studio 2022 Preview. The code
will expect a /data/ or C:\data\ directory to be present to store its 
configuration files.