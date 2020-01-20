# Companion Cube [![Emzi's Central Dispatch](https://discordapp.com/api/guilds/207879549394878464/widget.png)](https://discord.gg/rGKrJDR)
Companion Cube is a Discord bot built on top of [DSharpPlus library](https://github.com/DSharpPlus/DSharpPlus). 
It was primarily designed to replace Abalware™ but has since evolved to 
fulfill other functions.

Originally the base for [Music Turret "fork"](https://github.com/Emzi0767/Discord-Music-Turret-Bot), 
it has since received several feature backports, which greatly modernised its 
codebase.

More information is available on [its documentation page](https://emzi0767.com/#!/discord/companion-cube).

## Requirements
In order to run the bot you will need to have the following components 
installed and available on your system:
- .NET Core 3.x runtime or better
- Python 3.6 or better
- Java 8 or better
- PostgreSQL server 9.6 or better
- [Lavalink](https://github.com/Frederikam/Lavalink)

The bot was designed for UNIX-like environments, and is not guaranteed to work 
under Windows.

## Building
The bot requires that you have .NET Core 3.0 SDK, and preferably Visual Studio 
2019 installed and available on your system.

The required NuGet configurations are available in solution's root directory, 
so no further NuGet configuration should be required. Should that happen to not 
be the case, however, add the following MyGet feed to your NuGet sources:
`https://nuget.emzi0767.com/api/v3/index.json`

### Visual Studio 2017
Just open the solution and hit build, then publish. This will create a complete 
bot distribution in `bin/Release/netcoreapp3.0/publish/`.

### .NET Core SDK command line
Navigate to where the solution is located. From there you need to restore 
packages, build, and publish:
- `dotnet restore`
- `dotnet build -c Release`
- `dotnet publish -c Release -f netcoreapp3.0 -r linux-x64`

## Setting up
If you have all the required components installed, you need to properly set 
your environment up before running the bot.

I strongly recommend using Docker or another isolation/containerization 
solution to contain your bot.

### Step 1: PostgreSQL
You will need to log in to your PostgreSQL server, and create both a user and 
a database for the bot's data. This is typically done via `psql` utility.

After connecting to your database, you will first need to create a user for the 
bot to authenticate as:
`create user companion_cube with nocreatedb nocreaterole encrypted password 'hunter2';`
Do not forget to substitute the username and password with your own values.

Next step is to create a database for the database for the bot's data:
`create database companion_cube with owner='companion_cube';` Of course, don't 
forget to set your own database name, and set the owner to the username you 
created.

Finally, you need to input the schema into the database. Exit the `psql` 
utility and copy the schema files from Database directory to your server. Next 
up, open up a shell on your server, navigate to where you dropped the SQL 
files, and execute the following:
`find . -iname '*.sql' -exec cat "{}" \; | PGPASSWORD="hunter2" psql -U companion_cube -d companion_cube -h localhost`
This command will load all schema files to your database. Don't forget to set 
your password, username, database name, and hostname correctly.

### Step 2: Lavalink
Download and extract Lavalink. Copy the `lavalink.sh` script from this repo's 
Scripts directory to Lavalink's, then make it executable via 
`chmod +x lavalink.sh`.

Then follow the Lavalink's installation instructions, filling the config with 
proper values.

### Step 3: YouTube API
Open your browser and go to [Google Developer Console](https://console.developers.google.com/). 
There, create a new project. 

Once the project is created, go to its dashboard, and from there to API 
library. Find YouTube Data API v3, and enable it. 

When you enable the API, go to Credentials tab, and press Create Credentials.
Select API key as type. Copy the created API key and save it for later.

You can optionally give it a name and restrict its usage.

### Step 4: Discord API
Go to [Discord Developers page](https://discordapp.com/developers/applications/) 
and create a new app for the bot. Give it a name and an icon, and press Save 
Changes. When changes are saved, go to Bot tab, and press Add Bot. Give the bot 
a username and avatar, and uncheck the Public Bot checkbox, then press Save 
Changes.

Once changes are saved, press the Copy button under the token. Save the token 
for later.

### Step 5: Configuration
Copy all the files from your bot's publish directory to your target directory. 
Nextup, copy the `bot.sh` script from the repo's Scripts directory to the same 
place as your bot, and make it executable via `chmod +x bot.sh`.

Copy `config.json.example` from this repository to where the bot files are, and 
open it in your text editor.

Go to `discord` section, and paste the Discord API token from step 4 into the 
`token` field. You can optionally configure other options here. 

Once that's done, go to `postgres` section. Enter your PostgreSQL server data 
and database credentials. If your PostgreSQL server runs with SSL/TLS disabled, 
set `encrypt` to `false`.

Next, go to `lavalink` section, and enter your Lavalink connection details and 
the password you set when setting up Lavalink.

Finally, go to `youtube` section, and paste the API key you obtained in step 3.

When all is done, press save.

### Step 6: Set up Unicode data
Download the [Unicode database](https://unicode.org/Public/10.0.0/ucd/UCD.zip) and 
[Unihan database](https://unicode.org/Public/10.0.0/ucd/Unihan.zip). Extract 
`UnicodeData.txt` and `Blocks.txt` from the first file, and 
`Unihan_Readings.txt` from the second. Place the extracted files in Tools 
directory.

Next up, run the `mkjsondata.py` script from Tools (it requires Python 3). 
Once the script is done working, copy the resulting `unicode_data.json.gz` file
to bot's directory.

### Step 7: Run Lavalink
Using your favourite container software or container multiplexer, start 
Lavalink by running the autorestart script: `./lavalink.sh`. Detach from the 
multiplexer.

### Step 8: Run the bot
Like above, using a multiplexer or a container, run the bot by doing 
`./bot.sh`. If everything works correctly, congratulations. If not, follow the 
instructions more carefully.

## Support me
Lots of effort went into making this bot, and sometimes even related software.

If you feel like I'm doing a good job, or just want to throw money at me, you 
can do so through any of the following:
- [Patreon](https://www.patreon.com/emzi0767)
- [PayPal](https://paypal.me/Emzi0767/5USD)

## Other questions
If you have other questions or would like to talk in general, feel free to 
visit my Discord server.

[![Emzi's Central Dispatch](https://discordapp.com/api/guilds/207879549394878464/embed.png?style=banner1)](https://discord.gg/rGKrJDR)
