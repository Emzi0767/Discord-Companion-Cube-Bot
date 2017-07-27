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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Emzi0767.CompanionCube.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("admin"), Aliases("botctl"), Description("Commands for controlling the bot's behaviour."), OwnerOrPermission(Permissions.ManageGuild)]
    public sealed class AdministrationModule
    {
        private DatabaseClient Database { get; }
        private SharedData Shared { get; }

        public AdministrationModule(DatabaseClient database, SharedData shared)
        {
            this.Database = database;
            this.Shared = shared;
        }

        [Command("sudo"), Description("Executes a command as another user."), Hidden, RequireOwner]
        public async Task SudoAsync(CommandContext ctx, [Description("Member to execute the command as.")] DiscordMember member, [RemainingText, Description("Command text to execute.")] string command)
        {
            await ctx.CommandsNext.SudoAsync(member, ctx.Channel, command).ConfigureAwait(false);
        }

        [Command("sql"), Description("Executes a raw SQL query."), Hidden, RequireOwner]
        public async Task SqlQueryAsync(CommandContext ctx, [RemainingText, Description("SQL query to execute.")] string query)
        {
            var dat = await this.Database.ExecuteRawQueryAsync(query).ConfigureAwait(false);
            DiscordEmbed embed = null;

            if (!dat.Any() || !dat.First().Any())
            {
                embed = new DiscordEmbed
                {
                    Title = "Given query produced no results.",
                    Description = string.Concat("Query: ", Formatter.InlineCode(query), "."),
                    Color = 0x007FFF
                };
                await ctx.RespondAsync("", embed: embed).ConfigureAwait(false);
                return;
            }

            var d0 = dat.First().Select(xd => xd.Key).OrderByDescending(xs => xs.Length).First().Length + 1;

            embed = new DiscordEmbed
            { 
                Title = string.Concat("Results: ", dat.Count.ToString("#,##0")), 
                Description = string.Concat("Showing ", dat.Count > 24 ? "first 24" : "all", " results for query ", Formatter.InlineCode(query), ":"), 
                Color = 0x007FFF
            };
            var adat = dat.Take(24);

            var i = 0;
            foreach (var xdat in adat)
            {
                var sb = new StringBuilder();

                foreach (var (k, v) in xdat)
                    sb.Append(k).Append(new string(' ', d0 - k.Length)).Append("| ").AppendLine(v);

                embed.Fields.Add(new DiscordEmbedField
                {
                    Name = string.Concat("Result #", i++),
                    Value = Formatter.BlockCode(sb.ToString()),
                    Inline = false
                });
            }

            if (dat.Count > 24)
                embed.Fields.Add(new DiscordEmbedField 
                { 
                    Name = "Display incomplete", 
                    Value = string.Concat((dat.Count - 24).ToString("#,##0"), " results were omitted."), 
                    Inline = false 
                });
            
            await ctx.RespondAsync("", embed: embed).ConfigureAwait(false);
        }

        [Command("eval"), Description("Evaluates a snippet of C# code, in context."), Hidden, RequireOwner]
        public async Task EvaluateAsync(CommandContext ctx, [RemainingText, Description("Code to evaluate.")] string code)
        {
            var cs1 = code.IndexOf("```") + 3;
            cs1 = code.IndexOf('\n', cs1) + 1;
            var cs2 = code.LastIndexOf("```");

            if (cs1 == -1 || cs2 == -1)
                throw new ArgumentException("You need to wrap the code into a code block.");

            code = code.Substring(cs1, cs2 - cs1);

            var embed = new DiscordEmbed
            {
                Title = "Evaluating...",
                Color = 0xD091B2
            };
            var msg = await ctx.RespondAsync("", embed: embed).ConfigureAwait(false);

            var globals = new EvaluationEnvironment(ctx);
            var sopts = ScriptOptions.Default
                .WithImports("System", "System.Collections.Generic", "System.Linq", "System.Net.Http", "System.Net.Http.Headers", "System.Reflection", "System.Text", "System.Threading.Tasks", 
                    "DSharpPlus", "DSharpPlus.CommandsNext", "Emzi0767.CompanionCube", "Emzi0767.CompanionCube.Modules", "Emzi0767.CompanionCube.Services")
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));
            
            var sw1 = Stopwatch.StartNew();
            var cs = CSharpScript.Create(code, sopts, typeof(EvaluationEnvironment));
            var csc = cs.Compile();
            sw1.Stop();
            
            if (csc.Any(xd => xd.Severity == DiagnosticSeverity.Error))
            {
                embed = new DiscordEmbed
                {
                    Title = "Compilation failed",
                    Description = string.Concat("Compilation failed after ", sw1.ElapsedMilliseconds.ToString("#,##0"), "ms with ", csc.Length.ToString("#,##0"), " errors."),
                    Color = 0xD091B2,
                    Fields = new List<DiscordEmbedField>()
                };
                foreach (var xd in csc.Take(3))
                {
                    var ls = xd.Location.GetLineSpan();
                    embed.Fields.Add(new DiscordEmbedField
                    {
                        Name = string.Concat("Error at ", ls.StartLinePosition.Line.ToString("#,##0"), ", ", ls.StartLinePosition.Character.ToString("#,##0")),
                        Value = Formatter.InlineCode(xd.GetMessage()),
                        Inline = false
                    });
                }
                if (csc.Length > 3)
                {
                    embed.Fields.Add(new DiscordEmbedField
                    {
                        Name = "Some errors ommited",
                        Value = string.Concat((csc.Length - 3).ToString("#,##0"), " more errors not displayed"),
                        Inline = false
                    });
                }
                await msg.EditAsync(embed: embed).ConfigureAwait(false);
                return;
            }

            Exception rex = null;
            ScriptState<object> css = null;
            var sw2 = Stopwatch.StartNew();
            try
            {
                css = await cs.RunAsync(globals).ConfigureAwait(false);
                rex = css.Exception;
            }
            catch (Exception ex)
            {
                rex = ex;
            }
            sw2.Stop();

            if (rex != null)
            {
                embed = new DiscordEmbed
                {
                    Title = "Execution failed",
                    Description = string.Concat("Execution failed after ", sw2.ElapsedMilliseconds.ToString("#,##0"), "ms with `", rex.GetType(), ": ", rex.Message, "`."),
                    Color = 0xD091B2,
                };
                await msg.EditAsync(embed: embed).ConfigureAwait(false);
                return;
            }

            // execution succeeded
            embed = new DiscordEmbed
            {
                Title = "Evaluation successful",
                Color = 0xD091B2,
                Fields = new List<DiscordEmbedField>()
                {
                    new DiscordEmbedField
                    {
                        Name = "Result",
                        Value = css.ReturnValue != null ? css.ReturnValue.ToString() : "No value returned",
                        Inline = false
                    },
                    new DiscordEmbedField
                    {
                        Name = "Compilation time",
                        Value = string.Concat(sw1.ElapsedMilliseconds.ToString("#,##0"), "ms"),
                        Inline = true
                    },
                    new DiscordEmbedField
                    {
                        Name = "Execution time",
                        Value = string.Concat(sw2.ElapsedMilliseconds.ToString("#,##0"), "ms"),
                        Inline = true
                    }
                }
            };
            if (css.ReturnValue != null)
            {
                embed.Fields.Add(new DiscordEmbedField
                {
                    Name = "Return type",
                    Value = css.ReturnValue.GetType().ToString(),
                    Inline = true
                });
            }
            await msg.EditAsync(embed: embed).ConfigureAwait(false);
        }

        [Command("nick"), Aliases("nickname"), Description("Changes the bot's nickname."), OwnerOrPermission(Permissions.ManageNicknames)]
        public async Task NicknameAsync(CommandContext ctx, [Description("New nickname for the bot.")] string new_nickname = "")
        {
            var mbr = ctx.Guild.Members.FirstOrDefault(xm => xm.Id == ctx.Client.CurrentUser.Id) ?? await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id).ConfigureAwait(false);
            await mbr.ModifyAsync(new_nickname, reason: string.Concat("Edited by ", ctx.User.Username, "#", ctx.User.Discriminator, " (", ctx.User.Id, ")")).ConfigureAwait(false);
            await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
        }

        [Group("prefix"), Description("Commands for managing bot's command prefixes.")]
        public sealed class PrefixAdministration
        {
            private static readonly string alphabet = "abcdefghijklmnopqrstuvwxyzABCEDFGHIJKLMNOPQRSTUVWXYZ0123456789<>,./?;:'\"\\|[{]}=+-_!@#$%^&*()€";

            private DatabaseClient Database { get; }
            private SharedData Shared { get; }

            public PrefixAdministration(DatabaseClient database, SharedData shared)
            {
                this.Database = database;
                this.Shared = shared;
            }

            [Command("channel"), Description("Sets a new prefix for the channel the command is invoked in."), OwnerOrPermission(Permissions.ManageChannels)]
            public async Task ChannelAsync(CommandContext ctx, [RemainingText, Description("New prefix for this channel. Specifying null, empty string, or no value will reset the prefix.")] string new_prefix = null)
            {
                if (string.IsNullOrWhiteSpace(new_prefix))
                {
                    await this.Database.ResetChannelPrefixAsync(ctx.Channel.Id).ConfigureAwait(false);
                    this.Shared.ChannelPrefixes.TryRemove(ctx.Channel.Id, out _);
                }
                else if (new_prefix.Length > 6 || !new_prefix.All(xc => alphabet.Contains(xc)))
                {
                    throw new ArgumentException("Prefix must be less than or 6 characters long, and can consist only of characters available on the standard US keyboard.", nameof(new_prefix));
                }
                else
                {
                    await this.Database.SetChannelPrefixAsync(ctx.Channel.Id, new_prefix).ConfigureAwait(false);
                    this.Shared.ChannelPrefixes.AddOrUpdate(ctx.Channel.Id, new_prefix, (key, oldval) => new_prefix);
                }

                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
            }

            [Command("guild"), Description("Sets a new prefix for the guild the command is invoked in."), OwnerOrPermission(Permissions.ManageGuild)]
            public async Task GuildAsync(CommandContext ctx, [RemainingText, Description("New prefix for this guild. Specifying null, empty string, or no value will reset the prefix.")] string new_prefix = null)
            {
                if (string.IsNullOrWhiteSpace(new_prefix))
                {
                    await this.Database.ResetGuildPrefixAsync(ctx.Guild.Id).ConfigureAwait(false);
                    this.Shared.GuildPrefixes.TryRemove(ctx.Guild.Id, out _);
                }
                else if (new_prefix.Length > 6 || !new_prefix.All(xc => alphabet.Contains(xc)))
                {
                    throw new ArgumentException("Prefix must be less than or 6 characters long, and can consist only of characters available on the standard US keyboard.", nameof(new_prefix));
                }
                else
                {
                    await this.Database.SetGuildPrefixAsync(ctx.Guild.Id, new_prefix).ConfigureAwait(false);
                    this.Shared.GuildPrefixes.AddOrUpdate(ctx.Guild.Id, new_prefix, (key, oldval) => new_prefix);
                }

                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
            }
        }

        [Group("user"), Description("Commands for blocking and unblocking users from using the bot."), Hidden, RequireOwner]
        public sealed class UserAdministration
        {
            private DatabaseClient Database { get; }
            private SharedData Shared { get; }

            public UserAdministration(DatabaseClient database, SharedData shared)
            {
                this.Database = database;
                this.Shared = shared;
            }

            [Command("block"), Description("Blocks a user from using the bot.")]
            public async Task BlockAsync(CommandContext ctx, [Description("User to block.")] DiscordUser user)
            {
                await this.Database.BlockUserAsync(user.Id).ConfigureAwait(false);
                this.Shared.BlockedUsers.TryAdd(user.Id);
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
            }

            [Command("unblock"), Description("Unblocks a user, allowing them to use the bot again.")]
            public async Task UnblockAsync(CommandContext ctx, [Description("User to unblock.")] DiscordUser user)
            {
                await this.Database.UnblockUserAsync(user.Id).ConfigureAwait(false);
                this.Shared.BlockedUsers.TryRemove(user.Id);
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
            }
        }

        [Group("channel"), Description("Commands for blocking and unblocking bot from listening in specific channels."), OwnerOrPermission(Permissions.ManageChannels)]
        public sealed class ChannelAdministration
        {
            private DatabaseClient Database { get; }
            private SharedData Shared { get; }

            public ChannelAdministration(DatabaseClient database, SharedData shared)
            {
                this.Database = database;
                this.Shared = shared;
            }

            [Command("blockcurrent"), Aliases("block_current"), Description("Stops the bot from listening in the channel the command is invoked in.")]
            public async Task BlockCurrentAsync(CommandContext ctx)
            {
                await this.Database.BlockChannelAsync(ctx.Channel.Id).ConfigureAwait(false);
                this.Shared.BlockedChannels.TryAdd(ctx.Channel.Id);
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
            }

            [Command("block"), Description("Stops the bot from listening in specified channel."), Hidden, RequireOwner]
            public async Task BlockAsync(CommandContext ctx, [Description("Channel to block.")] DiscordChannel channel)
            {
                await this.Database.BlockChannelAsync(channel.Id).ConfigureAwait(false);
                this.Shared.BlockedChannels.TryAdd(channel.Id);
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
            }

            [Command("unblockcurrent"), Aliases("unblock_current"), Description("Makes the bot resume listening in the channel the command is invoked in.")]
            public async Task UnblockCurrentAsync(CommandContext ctx)
            {
                await this.Database.UnblockChannelAsync(ctx.Channel.Id).ConfigureAwait(false);
                this.Shared.BlockedChannels.TryRemove(ctx.Channel.Id);
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
            }

            [Command("unblock"), Description("Makes the bot resume listening in specified channel."), Hidden, RequireOwner]
            public async Task UnblockAsync(CommandContext ctx, [Description("Channel to unblock.")] DiscordChannel channel)
            {
                await this.Database.UnblockChannelAsync(channel.Id).ConfigureAwait(false);
                this.Shared.BlockedChannels.TryRemove(channel.Id);
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
            }
        }

        [Group("guild"), Description("Commands for blocking and unblocking the bot from listening in specific guilds."), Hidden, RequireOwner]
        public sealed class GuildAdministration
        {
            private DatabaseClient Database { get; }
            private SharedData Shared { get; }

            public GuildAdministration(DatabaseClient database, SharedData shared)
            {
                this.Database = database;
                this.Shared = shared;
            }
        
            [Command("block"), Description("Blocks a specific guild from interacting with the bot.")]
            public async Task BlockAsync(CommandContext ctx, [Description("Guild to block.")] DiscordGuild guild)
            {
                await this.Database.BlockGuildAsync(guild.Id).ConfigureAwait(false);
                this.Shared.BlockedGuilds.TryAdd(guild.Id);
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
            }

            [Command("unblock"), Description("Unblocks a specific guild from interacting with the bot.")]
            public async Task UnblockAsync(CommandContext ctx, [Description("Guild to unblock.")] DiscordGuild guild)
            {
                await this.Database.UnblockGuildAsync(guild.Id).ConfigureAwait(false);
                this.Shared.BlockedGuilds.TryRemove(guild.Id);
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
            }
        }
    }

    public sealed class EvaluationEnvironment
    {
        public CommandContext Context { get; }

        public DiscordMessage Message { get { return this.Context.Message; } }
        public DiscordChannel Channel { get { return this.Context.Channel; } }
        public DiscordGuild Guild { get { return this.Context.Guild; } }
        public DiscordUser User { get { return this.Context.User; } }
        public DiscordMember Member { get { return this.Context.Member; } }
        public DiscordClient Client { get { return this.Context.Client; } }

        public EvaluationEnvironment(CommandContext ctx)
        {
            this.Context = ctx;
        }
    }
}