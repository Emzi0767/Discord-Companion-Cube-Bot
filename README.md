# Companion Cube by Emzi0767

[![Emzi's Central Dispatch](https://discordapp.com/api/guilds/207879549394878464/widget.png)](https://discord.gg/rGKrJDR)

## ABOUT

Companion Cube is a Discord bot built on top of [DSharpPlus library](https://github.com/NaamloosDT/DSharpPlus). It was primarily designed to replace Abalware™ in Discord API, but during development I also decided to add some fun features (such as currency).

More information is available on [its GitHub page](https://emzi0767.github.io/discord/companion_cube/).

## BUILDING

You need .NET Core SDK 2.0 Preview 2 to build the project, and .NET Core 2.0.0-preview3-25518-01 runtime to run it.

1. In order to build this project, you will need to add the following package sources to your NuGet:
   * `https://www.myget.org/F/discord-net/api/v3/index.json`
   * `https://dotnet.myget.org/F/roslyn/api/v3/index.json`
2. Next, you must restore all NuGet packages (`dotnet restore`).
3. Then build the code in Release mode (`dotnet build -c Release`).
4. Finally publish the bot (`dotnet publish -c Release -f netcoreapp2.0`).

## SETUP

In order for bot to run, you will need to set up your environment. 

### POSTGRESQL DATABASE

1. If you haven't done so already, install PostgreSQL server (version 9.6 or better).
2. Create a database for bot's data.
3. Create a user for the database.
4. Execute the attached `schema_v1.sql` script as the created user.
5. Execute `CREATE EXTENSION fuzzystrmatch;` as `postgres` user in the database.

### THE BOT ITSELF

1. Create a directory for the bot.
2. Copy the publish results to the directory.
3. Run the bot (`dotnet Emzi0767.CompanionCube.dll`). This will generate an empty config file.
4. Fill the config file with proper values.

## RUNNING THE BOT

Execute `dotnet Emzi0767.CompanionCube.dll`. That's it, the bot is running.

It is recommended you run the bot in a terminal multiplexer, such as `screen` or `tmux` when running on GNU/Linux.

## SUPPORT ME

If you feel like supporting me by providing me with currency that I can exchange for goods and services, you can do so on [my Patreon](https://www.patreon.com/emzi0767).

## ADDITIONAL HELP

Should you still have any questions regarding the bot, feel free to join my server. I'll try to answer an questions:

[![Emzi's Central Dispatch](https://discordapp.com/api/guilds/207879549394878464/embed.png?style=banner1)](https://discord.gg/rGKrJDR)