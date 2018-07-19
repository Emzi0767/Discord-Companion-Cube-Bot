// This file is part of Emzi0767.CompanionCube project
//
// Copyright 2017 Emzi0767
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
using DSharpPlus.Net.Udp;
using Emzi0767.CompanionCube.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Emzi0767.CompanionCube
{
    public sealed class CompanionCubeCore
    {
        public const string LOG_TAG = "CompanionCube";
        private static readonly object _lock = new object();

        public int ShardId { get; }
        public CompanionCubeConfig Configuration { get; }
        private RandomNumberGenerator RNGesus { get; }
        private SharedData Shared { get; }
        private Timer GameTimer { get; set; }

        public DiscordClient Client { get; private set; }
        public CommandsNextExtension CommandsNext { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public LavalinkExtension Lavalink { get; private set; }
        public DatabaseClient Database { get; }

        public CompanionCubeCore(CompanionCubeConfig config, int shardId, DatabaseClient database, SharedData sharedData)
        {
            this.Configuration = config;
            this.ShardId = shardId;
            this.Database = database;
            this.RNGesus = RandomNumberGenerator.Create();
            
            this.Shared = sharedData;
        }

        public void Initialize()
        {
            // initialize the client
            var dcfg = new DiscordConfiguration
            {
                AutoReconnect = true,
                GatewayCompressionLevel = GatewayCompressionLevel.Stream,
                LargeThreshold = 250,
                LogLevel = LogLevel.Debug,

                MessageCacheSize = this.Configuration.MessageCacheSize,
                ShardCount = this.Configuration.ShardCount,
                ShardId = this.ShardId,
                UseInternalLogHandler = false,

                Token = this.Configuration.Token,
                TokenType = TokenType.Bot
            };
            this.Client = new DiscordClient(dcfg);

            this.Lavalink = this.Client.UseLavalink();

            // initialize cnext dependencies
            var deps = new ServiceCollection()
                .AddSingleton(this.Client)
                .AddSingleton(this.Database)
                .AddSingleton(this.Shared)
                .AddSingleton(new CSPRNG())
                .AddSingleton(this.Shared.Http);

            // initialize cnext
            var ccfg = new CommandsNextConfiguration
            {
                CaseSensitive = false,
                EnableDefaultHelp = true,
                EnableDms = false,
                DefaultHelpChecks = new List<CheckBaseAttribute>() { new NotBlockedAttribute() },

                Services = deps.BuildServiceProvider(),
                PrefixResolver = this.PrefixResolverAsync,
                EnableMentionPrefix = this.Configuration.EnableMentionPrefix,
            };
            this.CommandsNext = this.Client.UseCommandsNext(ccfg);

            // hook commands
            this.CommandsNext.RegisterCommands(Assembly.GetExecutingAssembly());

            // hook help formatter
            this.CommandsNext.SetHelpFormatter<CompanionCubeHelpFormatter>();

            // initialize interactivity
            this.Interactivity = this.Client.UseInteractivity(new InteractivityConfiguration());

            // hook events
            this.Client.DebugLogger.LogMessageReceived += this.LogMessageHandler;

            this.Client.GuildAvailable += this.OnGuildAvailable;
            this.Client.MessageCreated += this.OnMessageCreated;
            this.Client.ClientErrored += this.OnClientErrored;
            this.Client.SocketErrored += this.OnSocketErrored;
            this.Client.Ready += this.OnReady;

            this.CommandsNext.CommandExecuted += this.OnCommandExecuted;
            this.CommandsNext.CommandErrored += this.OnCommandErrored;
        }

        public async Task StartAsync()
        {
            this.Client.DebugLogger.LogMessage(LogLevel.Info, LOG_TAG, "Booting companion cube shard", DateTime.Now);
            await this.Client.ConnectAsync().ConfigureAwait(false);
        }

        private void LogMessageHandler(object sender, DebugLogMessageEventArgs ea)
        {
            lock (_lock)
            {
                Console.BackgroundColor = ConsoleColor.Black;

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("[{0:yyyy-MM-dd HH:mm:ss zzz}] ", ea.Timestamp);

                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("[SHARD {0}] ", this.ShardId.ToString());

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("[{0}] ", FixWidth(ea.Application, 13));

                var ccfg = ConsoleColor.Gray;
                var ccbg = ConsoleColor.Black;
                switch (ea.Level)
                {
                    case LogLevel.Critical:
                        ccfg = ConsoleColor.Black;
                        ccbg = ConsoleColor.Red;
                        break;
                    
                    case LogLevel.Error:
                        ccfg = ConsoleColor.Red;
                        break;

                    case LogLevel.Warning:
                        ccfg = ConsoleColor.Yellow;
                        break;

                    case LogLevel.Info:
                        ccfg = ConsoleColor.Cyan;
                        break;

                    case LogLevel.Debug:
                        ccfg = ConsoleColor.Magenta;
                        break;
                }
                Console.ForegroundColor = ccfg;
                Console.BackgroundColor = ccbg;
                Console.Write("[{0}]", FixWidth(ea.Level.ToString(), 8));
                
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(" ");
                Console.WriteLine(ea.Message);
            }
        }

        private Task OnGuildAvailable(GuildCreateEventArgs ea)
        {
            ea.Client.DebugLogger.LogMessage(LogLevel.Info, LOG_TAG, string.Concat("Guild available: ", ea.Guild.Name), DateTime.Now);
            return Task.CompletedTask;
        }

        private async Task OnMessageCreated(MessageCreateEventArgs ea)
        {
            if (ea.Channel.Type != ChannelType.Text || ea.Guild == null || ea.Author.IsBot || this.Shared.BlockedChannels.Contains(ea.Channel.Id) || this.Shared.BlockedGuilds.Contains(ea.Guild.Id))
                return; // nothing to see here, move along

            var rates = this.Shared.ShekelRates;
            if (!rates.TryGetValue(ea.Guild.Id, out var rate))
                rate = 0.05;
            
            var b = new byte[4];
            this.RNGesus.GetBytes(b);
            var d = (double)BitConverter.ToUInt32(b, 0) / (double)uint.MaxValue;
            if (d >= (1.0 - rate))
                await this.Database.IssueCurrencyAsync(ea.Author.Id, 1);
        }

        private Task OnClientErrored(ClientErrorEventArgs ea)
        {
            var ex = ea.Exception;
            while (ex is AggregateException)
                ex = ex.InnerException;
            
            ea.Client.DebugLogger.LogMessage(LogLevel.Error, LOG_TAG, string.Concat(ea.EventName, " threw an exception ", ex.GetType(), ": ", ex.Message), DateTime.Now);
            return Task.CompletedTask;
        }

        private Task OnSocketErrored(SocketErrorEventArgs ea)
        {
            var ex = ea.Exception;
            while (ex is AggregateException)
                ex = ex.InnerException;
            
            ea.Client.DebugLogger.LogMessage(LogLevel.Critical, LOG_TAG, string.Concat("Socket threw an exception ", ex.GetType(), ": ", ex.Message), DateTime.Now);
            return Task.CompletedTask;
        }

        private async Task OnReady(ReadyEventArgs ea)
        {
            var llc = this.Configuration.LavalinkConfig.ToLavalinkConfig();
            await this.Lavalink.ConnectAsync(llc);

            this.GameTimer = new Timer(this.GameTimerCallback, ea.Client, TimeSpan.Zero, TimeSpan.FromMinutes(15));
        }

        private Task OnCommandExecuted(CommandExecutionEventArgs ea)
        {
            ea.Context.Client.DebugLogger.LogMessage(LogLevel.Info, LOG_TAG,
                string.Concat("User '", ea.Context.User.Username, "#", ea.Context.User.Discriminator, "' (", ea.Context.User.Id, ") executed '", ea.Command.QualifiedName, "' in #",
                    ea.Context.Channel.Name, " (", ea.Context.Channel.Id, ")"), DateTime.Now);
            return Task.CompletedTask;
        }

        private async Task OnCommandErrored(CommandErrorEventArgs ea)
        {
            ea.Context.Client.DebugLogger.LogMessage(LogLevel.Error, LOG_TAG, 
                string.Concat("User '", ea.Context.User.Username, "#", ea.Context.User.Discriminator, "' (", ea.Context.User.Id, ") tried to execute '", 
                    ea.Command?.QualifiedName ?? "<unknown command>", "' in #", ea.Context.Channel.Name, " (", ea.Context.Channel.Id, ") and failed with ", ea.Exception.GetType(), ": ", 
                    ea.Exception.Message), DateTime.Now);
            DiscordEmbedBuilder embed = null;

            var ex = ea.Exception;
            while (ex is AggregateException)
                ex = ex.InnerException;
            
            if (ex is ChecksFailedException cfe)
            {
                if (!cfe.FailedChecks.OfType<NotBlockedAttribute>().Any())
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Permission denied",
                        Description = string.Concat(DiscordEmoji.FromName(ea.Context.Client, ":msraisedhand:"), " You lack permissions necessary to run this command."),
                        Color = new DiscordColor(0xFF0000)
                    };
            }
            else if (ex is CommandNotFoundException)
            {
                // ignore
            }
            else
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "A problem occured while executing the command",
                    Description = string.Concat(Formatter.InlineCode(ea.Command.QualifiedName), " threw an exception: `", ex.GetType(), ": ", ex.Message, "`"),
                    Color = new DiscordColor(0xFF0000)
                };
            }

            if (embed != null)
                await ea.Context.RespondAsync("", embed: embed.Build());
        }

        private Task<int> PrefixResolverAsync(DiscordMessage m)
        {
            if (!this.Shared.GuildPrefixes.TryGetValue(m.Channel.Guild.Id, out var prefix) && !this.Shared.ChannelPrefixes.TryGetValue(m.Channel.Id, out prefix))
                prefix = this.Configuration.DefaultCommandPrefix;
            return Task.FromResult(m.GetStringPrefixLength(prefix));
        }

        private void GameTimerCallback(object _)
        {
            var client = _ as DiscordClient;
            try
            {
                client.UpdateStatusAsync(new DiscordActivity(this.Shared.Game)).ConfigureAwait(false).GetAwaiter().GetResult();
                client.DebugLogger.LogMessage(LogLevel.Info, LOG_TAG, "Presence updated", DateTime.Now);
            }
            catch (Exception) 
            { 
                client.DebugLogger.LogMessage(LogLevel.Error, LOG_TAG, "Could not update presence", DateTime.Now);
            }
        }

        private static string FixWidth(string str, int width)
        {
            if (str.Length < width)
                return string.Concat(str, new string(' ', width - str.Length));
            else if (str.Length > width)
                return str.Substring(0, width);
            return str;
        }
    }
}