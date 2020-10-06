// This file is part of Companion Cube project
//
// Copyright 2018 Emzi0767
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using Emzi0767.CompanionCube.Attributes;
using Emzi0767.CompanionCube.Data;
using Emzi0767.CompanionCube.Services;
using Emzi0767.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Emzi0767.CompanionCube
{
    /// <summary>
    /// Represents a single shard of the Companion Cube bot.
    /// </summary>
    public sealed class CompanionCubeBot
    {
        /// <summary>
        /// Gets the tag used when emitting log events from the bot.
        /// </summary>
        public static EventId LogEvent { get; } = new EventId(1000, "CCube");

        /// <summary>
        /// Gets the discord client instance for this bot shard.
        /// </summary>
        public DiscordClient Discord { get; }

        /// <summary>
        /// Gets the CommandsNext extension instance.
        /// </summary>
        public CommandsNextExtension CommandsNext { get; }

        /// <summary>
        /// Gets the Interactivity extension instance.
        /// </summary>
        public InteractivityExtension Interactivity { get; }

        /// <summary>
        /// Gets the Lavalink instance.
        /// </summary>
        public LavalinkExtension Lavalink { get; }

        /// <summary>
        /// Gets the ID of this shard.
        /// </summary>
        public int ShardId { get; }

        /// <summary>
        /// Gets the version of the bot.
        /// </summary>
        public string BotVersion { get; }

        /// <summary>
        /// Gets the configuration of this bot.
        /// </summary>
        public CompanionCubeConfig Configuration { get; }

        private ConnectionStringProvider ConnectionStringProvider { get; }
        private AsyncExecutor AsyncExecutor { get; }
        private Timer GameTimer { get; set; } = null;
        private IServiceProvider Services { get; }
        
        /// <summary>
        /// Creates a new instance of Companion Cube bot shard handler.
        /// </summary>
        /// <param name="cfg">Configuration options for the shard.</param>
        /// <param name="shardId">ID of this shard.</param>
        /// <param name="async">Synchronous executor of asynchronous tasks.</param>
        public CompanionCubeBot(CompanionCubeConfig cfg, int shardId, AsyncExecutor async)
        {
            // assign the properties
            this.ShardId = shardId;
            this.BotVersion = CompanionCubeUtilities.GetBotVersion();
            this.Configuration = cfg;
            this.ConnectionStringProvider = new ConnectionStringProvider(cfg.PostgreSQL);
            this.AsyncExecutor = async;

            // create discord client instance
            this.Discord = new DiscordClient(new DiscordConfiguration
            {
                Token = cfg.Discord.Token,
                TokenType = TokenType.Bot,
                ShardCount = cfg.Discord.ShardCount,
                ShardId = this.ShardId,

                AutoReconnect = true,
                ReconnectIndefinitely = true,
                GatewayCompressionLevel = GatewayCompressionLevel.Stream,
                LargeThreshold = 250
            });

            // attach event handlers
            this.Discord.Ready += this.Discord_Ready;
            this.Discord.GuildDownloadCompleted += this.Discord_GuildDownloadCompleted;
            this.Discord.SocketErrored += this.Discord_SocketErrored;
            this.Discord.GuildAvailable += this.Discord_GuildAvailable;
            this.Discord.VoiceStateUpdated += this.Discord_VoiceStateUpdated;

            // create service provider
            this.Services = new ServiceCollection()
                .AddTransient<SecureRandom>()
                .AddSingleton(this.ConnectionStringProvider)
                .AddSingleton<MusicService>()
                .AddScoped<DatabaseContext>()
                .AddSingleton(new LavalinkService(cfg.Lavalink, this.Discord))
                .AddSingleton(new YouTubeSearchProvider(cfg.YouTube))
                .AddSingleton<HttpClient>()
                .AddSingleton(this)
                .AddSingleton<FeedTimerService>()
                .AddScoped<FeedService>()
                .AddSingleton(this.Discord)
                .AddSingleton(new PooperService(this.Discord, this.ConnectionStringProvider))
                .BuildServiceProvider(true);

            // create CommandsNext
            this.CommandsNext = this.Discord.UseCommandsNext(new CommandsNextConfiguration
            {
                CaseSensitive = false,
                EnableDms = false,
                IgnoreExtraArguments = false,

                EnableDefaultHelp = true,
                DefaultHelpChecks = new[] { new NotBlacklistedAttribute() },

                EnableMentionPrefix = cfg.Discord.EnableMentionPrefix,
                PrefixResolver = this.ResolvePrefixAsync,

                Services = this.Services
            });

            // set help formatter
            this.CommandsNext.SetHelpFormatter<CompanionCubeHelpFormatter>();

            // register type converters
            this.CommandsNext.RegisterConverter(new TagTypeConverter());
            this.CommandsNext.RegisterConverter(new HansToolConverter());
            this.CommandsNext.RegisterUserFriendlyTypeName<TagType>("tag type");
            this.CommandsNext.RegisterUserFriendlyTypeName<HansTool>("hans tool");

            // attach event handlers
            this.CommandsNext.CommandExecuted += this.CommandsNext_CommandExecuted;
            this.CommandsNext.CommandErrored += this.CommandsNext_CommandErrored;

            // create commands
            this.CommandsNext.RegisterCommands(Assembly.GetExecutingAssembly());

            // create interactivity
            this.Interactivity = this.Discord.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromSeconds(30)
            });

            // create lavalink
            this.Lavalink = this.Discord.UseLavalink();

            // add issue handler
            this.Discord.AddExtension(new IssueHandlerExtension(cfg.GitHub));
        }

        /// <summary>
        /// Signals the Discord client to connect to API and starts the bot.
        /// </summary>
        /// <returns></returns>
        public Task StartAsync()
        {
            this.Discord.Logger.LogInformation(LogEvent, "Booting Companion Cube shard.", DateTime.Now);
            return this.Discord.ConnectAsync();
        }

        private Task Discord_Ready(DiscordClient sender, ReadyEventArgs e)
        {
            sender.Logger.LogInformation(LogEvent, "Client is ready to process events", DateTime.Now);

            if (this.GameTimer == null && !string.IsNullOrWhiteSpace(this.Configuration.Discord.Game))
                this.GameTimer = new Timer(this.GameTimerCallback, sender, TimeSpan.Zero, TimeSpan.FromHours(1));

            return Task.CompletedTask;
        }

        private Task Discord_GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e)
        {
            this.Services.GetRequiredService<FeedTimerService>().Start();

            sender.Logger.LogInformation(LogEvent, "All guilds are now available", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Discord_SocketErrored(DiscordClient sender, SocketErrorEventArgs e)
        {
            var ex = e.Exception;
            while (ex is AggregateException)
                ex = ex.InnerException;

            sender.Logger.LogCritical(LogEvent, $"Socket threw an exception {ex.GetType()}: {ex.Message}", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Discord_GuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            sender.Logger.LogInformation(LogEvent, $"Guild available: {e.Guild.Name}", DateTime.Now);
            return Task.CompletedTask;
        }

        private async Task Discord_VoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
        {
            if (e.User == this.Discord.CurrentUser)
                return;

            var music = this.Services.GetService<MusicService>();
            var gmd = await music.GetOrCreateDataAsync(e.Guild);
            var chn = gmd.Channel;
            if (chn == null || chn != e.Before.Channel)
                return;

            var usrs = chn.Users;
            if (gmd.IsPlaying && !usrs.Any(x => !x.IsBot))
            {
                sender.Logger.LogInformation(LogEvent, $"All users left voice in {e.Guild.Name}, pausing playback", DateTime.Now);
                await gmd.PauseAsync();

                if (gmd.CommandChannel != null)
                    await gmd.CommandChannel.SendMessageAsync($"{DiscordEmoji.FromName(sender, ":play_pause:")} All users left the channel, playback paused. You can resume it by joining the channel and using the `resume` command.");
            }
        }

        private Task CommandsNext_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            e.Context.Client.Logger.LogInformation(LogEvent,
                $"User '{e.Context.User.Username}#{e.Context.User.Discriminator}' ({e.Context.User.Id}) executed '{e.Command.QualifiedName}' in #{e.Context.Channel.Name} ({e.Context.Channel.Id})",
                DateTime.Now);
            return Task.CompletedTask;
        }

        private async Task CommandsNext_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            e.Context.Client.Logger.LogError(LogEvent,
                $"User '{e.Context.User.Username}#{e.Context.User.Discriminator}' ({e.Context.User.Id}) tried to execute '{e.Command?.QualifiedName ?? "<unknown command>"}' "
                + $"in #{e.Context.Channel.Name} ({e.Context.Channel.Id}) and failed with {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);
            DiscordEmbedBuilder embed = null;

            var ex = e.Exception;
            while (ex is AggregateException)
                ex = ex.InnerException;

            if (ex is CommandNotFoundException)
            { } // ignore
            else if (ex is CommandCancelledException)
            { } // ignore
            else if (ex is ChecksFailedException cfe)
            {
                if (!cfe.FailedChecks.Any(x => x is NotBlacklistedAttribute || x is RequirePrefixesAttribute))
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Permission denied",
                        Description = $"{DiscordEmoji.FromName(e.Context.Client, ":msraisedhand:")} You lack permissions necessary to run this command.",
                        Color = new DiscordColor(0xFF0000)
                    };
            }
            else
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "A problem occured while executing the command",
                    Description = $"{Formatter.InlineCode(e.Command.QualifiedName)} threw an exception: `{ex.GetType()}: {ex.Message}`",
                    Color = new DiscordColor(0xFF0000)
                };
            }

            if (embed != null)
                await e.Context.RespondAsync("", embed: embed.Build());
        }

        private Task<int> ResolvePrefixAsync(DiscordMessage msg)
        {
            var gld = msg.Channel.Guild;
            if (gld == null)
                return Task.FromResult(-1);

            var gldId = (long)gld.Id;
            using var db = new DatabaseContext(this.ConnectionStringProvider);
            var gpfix = db.Prefixes.SingleOrDefault(x => x.GuildId == gldId);
            if (gpfix != null)
            {
                foreach (var pfix in gpfix.Prefixes)
                {
                    var pfixLocation = msg.GetStringPrefixLength(pfix, StringComparison.OrdinalIgnoreCase);
                    if (pfixLocation != -1)
                        return Task.FromResult(pfixLocation);
                }

                if (gpfix.EnableDefault != true)
                    return Task.FromResult(-1);
            }

            foreach (var pfix in this.Configuration.Discord.DefaultPrefixes)
            {
                var pfixLocation = msg.GetStringPrefixLength(pfix, StringComparison.OrdinalIgnoreCase);
                if (pfixLocation != -1)
                    return Task.FromResult(pfixLocation);
            }

            return Task.FromResult(-1);
        }

        private void GameTimerCallback(object _)
        {
            var client = _ as DiscordClient;
            try
            {
                this.AsyncExecutor.Execute(client.UpdateStatusAsync(new DiscordActivity(this.Configuration.Discord.Game), UserStatus.Online, null));
                client.Logger.LogInformation(LogEvent, "Presence updated", DateTime.Now);
            }
            catch (Exception ex)
            {
                client.Logger.LogError(LogEvent, $"Could not update presence ({ex.GetType()}: {ex.Message})", DateTime.Now);
            }
        }
    }
}