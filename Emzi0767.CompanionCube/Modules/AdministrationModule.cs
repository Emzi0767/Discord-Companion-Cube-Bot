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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using Emzi0767.CompanionCube.Attributes;
using Emzi0767.CompanionCube.Data;
using Emzi0767.CompanionCube.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("admin"), Aliases("botctl")]
    [Description("Commands for controlling the bot's behaviour.")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [OwnerOrPermission(Permissions.ManageGuild)]
    public sealed class AdministrationModule : BaseCommandModule
    {
        private DatabaseContext Database { get; }
        private CompanionCubeBot Bot { get; }

        public AdministrationModule(DatabaseContext database, CompanionCubeBot bot)
        {
            this.Database = database;
            this.Bot = bot;
        }

        [Command("sudo"), Description("Executes a command as another user."), Hidden, RequireOwner]
        public async Task SudoAsync(CommandContext ctx, [Description("Member to execute the command as.")] DiscordMember member, [RemainingText, Description("Command text to execute.")] string command)
        {
            var cmd = ctx.CommandsNext.FindCommand(command, out var args);
            if (cmd == null)
                throw new CommandNotFoundException(command);

            var fctx = ctx.CommandsNext.CreateFakeContext(member, ctx.Channel, command, ctx.Prefix, cmd, args);
            await ctx.CommandsNext.ExecuteCommandAsync(fctx).ConfigureAwait(false);
        }

        [Command("sql"), Description("Executes a raw SQL query."), Hidden, RequireOwner]
        public async Task SqlQueryAsync(CommandContext ctx, [RemainingText, Description("SQL query to execute.")] string query)
        {
            var dat = new List<Dictionary<string, string>>();
            var i = 0;
            using (var cmd = this.Database.Database.GetDbConnection().CreateCommand())
            {
                await this.Database.Database.OpenConnectionAsync().ConfigureAwait(false);

                cmd.CommandText = query;
                using (var rdr = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await rdr.ReadAsync().ConfigureAwait(false))
                    {
                        var dict = new Dictionary<string, string>();
                        for (i = 0; i < rdr.FieldCount; i++)
                            dict[rdr.GetName(i)] = rdr[i] is DBNull ? "<null>" : rdr[i].ToString();
                        dat.Add(dict);
                    }
                }
            }
            
            DiscordEmbedBuilder embed = null;
            if (!dat.Any() || !dat.First().Any())
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Given query produced no results.",
                    Description = string.Concat("Query: ", Formatter.InlineCode(query), "."),
                    Color = new DiscordColor(0x007FFF)
                };
                await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
                return;
            }

            var d0 = dat.First().Select(xd => xd.Key).OrderByDescending(xs => xs.Length).First().Length + 1;

            embed = new DiscordEmbedBuilder
            { 
                Title = string.Concat("Results: ", dat.Count.ToString("#,##0")), 
                Description = string.Concat("Showing ", dat.Count > 24 ? "first 24" : "all", " results for query ", Formatter.InlineCode(query), ":"), 
                Color = new DiscordColor(0x007FFF)
            };
            var adat = dat.Take(24);

            i = 0;
            foreach (var xdat in adat)
            {
                var sb = new StringBuilder();

                foreach (var (k, v) in xdat)
                    sb.Append(k).Append(new string(' ', d0 - k.Length)).Append("| ").AppendLine(v);

                embed.AddField(string.Concat("Result #", i++), Formatter.BlockCode(sb.ToString()), false);
            }

            if (dat.Count > 24)
                embed.AddField("Display incomplete", string.Concat((dat.Count - 24).ToString("#,##0"), " results were omitted."), false);
            
            await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("eval"), Description("Evaluates a snippet of C# code, in context."), Hidden, RequireOwner]
        public async Task EvaluateAsync(CommandContext ctx, [RemainingText, Description("Code to evaluate.")] string code)
        {
            var cs1 = code.IndexOf("```") + 3;
            cs1 = code.IndexOf('\n', cs1) + 1;
            var cs2 = code.LastIndexOf("```");

            if (cs1 == -1 || cs2 == -1)
                throw new ArgumentException("You need to wrap the code into a code block.", nameof(code));

            code = code.Substring(cs1, cs2 - cs1);

            var embed = new DiscordEmbedBuilder
            {
                Title = "Evaluating...",
                Color = new DiscordColor(0xD091B2)
            };
            var msg = await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);

            var globals = new EvaluationEnvironment(ctx);
            var sopts = ScriptOptions.Default
                .WithImports("System", "System.Collections.Generic", "System.Diagnostics", "System.Linq", "System.Net.Http", "System.Net.Http.Headers", "System.Reflection", "System.Text", 
                             "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.CommandsNext", "DSharpPlus.Entities", "DSharpPlus.EventArgs", "DSharpPlus.Exceptions", "Emzi0767.CompanionCube", 
                             "Emzi0767.CompanionCube.Modules", "Emzi0767.CompanionCube.Services")
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));
            
            var sw1 = Stopwatch.StartNew();
            var cs = CSharpScript.Create(code, sopts, typeof(EvaluationEnvironment));
            var csc = cs.Compile();
            sw1.Stop();
            
            if (csc.Any(xd => xd.Severity == DiagnosticSeverity.Error))
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Compilation failed",
                    Description = string.Concat("Compilation failed after ", sw1.ElapsedMilliseconds.ToString("#,##0"), "ms with ", csc.Length.ToString("#,##0"), " errors."),
                    Color = new DiscordColor(0xD091B2)
                };
                foreach (var xd in csc.Take(3))
                {
                    var ls = xd.Location.GetLineSpan();
                    embed.AddField(string.Concat("Error at ", ls.StartLinePosition.Line.ToString("#,##0"), ", ", ls.StartLinePosition.Character.ToString("#,##0")), Formatter.InlineCode(xd.GetMessage()), false);
                }
                if (csc.Length > 3)
                {
                    embed.AddField("Some errors ommited", string.Concat((csc.Length - 3).ToString("#,##0"), " more errors not displayed"), false);
                }
                await msg.ModifyAsync(embed: embed.Build()).ConfigureAwait(false);
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
                embed = new DiscordEmbedBuilder
                {
                    Title = "Execution failed",
                    Description = string.Concat("Execution failed after ", sw2.ElapsedMilliseconds.ToString("#,##0"), "ms with `", rex.GetType(), ": ", rex.Message, "`."),
                    Color = new DiscordColor(0xD091B2),
                };
                await msg.ModifyAsync(embed: embed.Build()).ConfigureAwait(false);
                return;
            }

            // execution succeeded
            embed = new DiscordEmbedBuilder
            {
                Title = "Evaluation successful",
                Color = new DiscordColor(0xD091B2),
            };

            embed.AddField("Result", css.ReturnValue != null ? css.ReturnValue.ToString() : "No value returned", false)
                .AddField("Compilation time", string.Concat(sw1.ElapsedMilliseconds.ToString("#,##0"), "ms"), true)
                .AddField("Execution time", string.Concat(sw2.ElapsedMilliseconds.ToString("#,##0"), "ms"), true);

            if (css.ReturnValue != null)
                embed.AddField("Return type", css.ReturnValue.GetType().ToString(), true);

            await msg.ModifyAsync(embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("nick"), Aliases("nickname"), Description("Changes the bot's nickname."), OwnerOrPermission(Permissions.ManageNicknames)]
        public async Task NicknameAsync(CommandContext ctx, [Description("New nickname for the bot.")] string new_nickname = "")
        {
            var mbr = ctx.Guild.Members.FirstOrDefault(xm => xm.Id == ctx.Client.CurrentUser.Id) ?? await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id).ConfigureAwait(false);
            await mbr.ModifyAsync(x =>
            {
                x.Nickname = new_nickname;
                x.AuditLogReason = string.Concat("Edited by ", ctx.User.Username, "#", ctx.User.Discriminator, " (", ctx.User.Id, ")");
            }).ConfigureAwait(false);
            await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
        }

        [Command("setmusic"), Description("Sets whether music in the specified guild shall be available or not."), RequireOwner]
        public async Task MusicAsync(CommandContext ctx,
            [Description("Guild for which to toggle the setting.")] DiscordGuild guild,
            [Description("Whether the music should be available.")] bool enabled,
            [RemainingText, Description("Reason why the guild has music enabled.")] string reason = null)
        {
            var gid = (long)guild.Id;
            var enable = this.Database.MusicEnabled.SingleOrDefault(x => x.GuildId == gid);
            if (enabled && enable == null)
            {
                enable = new DatabaseMusicEnabled
                {
                    GuildId = gid,
                    Reason = reason
                };
                this.Database.MusicEnabled.Add(enable);
            }
            else if (!enabled && enable != null)
            {
                this.Database.MusicEnabled.Remove(enable);
            }

            await this.Database.SaveChangesAsync().ConfigureAwait(false);
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Music is now {(enabled? "enabled" : "disabled")} in {Formatter.Bold(Formatter.Sanitize(guild.Name))}.").ConfigureAwait(false);
        }

        [Command("setblock"), Description("Sets blocked status for a user, channel, or guild."), Aliases("block", "unblock")]
        public async Task SetBlockAsync(CommandContext ctx,
           [Description("User whose block status to change.")] DiscordUser user,
           [Description("Whether the user should be blocked.")] bool blocked,
           [RemainingText, Description("Reason why this user is blocked.")] string reason = null)
        {
            var uid = (long)user.Id;
            var block = this.Database.BlockedEntities.SingleOrDefault(x => x.Id == uid && x.Kind == DatabaseEntityKind.User);
            if (blocked && block == null)
            {
                block = new DatabaseBlockedEntity
                {
                    Id = uid,
                    Kind = DatabaseEntityKind.User,
                    Reason = reason,
                    Since = DateTime.UtcNow
                };
                this.Database.BlockedEntities.Add(block);
            }
            else if (!blocked && block != null)
            {
                this.Database.BlockedEntities.Remove(block);
            }

            await this.Database.SaveChangesAsync().ConfigureAwait(false);
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} User {user.Mention} is {(blocked ? "now blocked" : "no longer blocked")}.").ConfigureAwait(false);
        }

        [Command("setblock")]
        public async Task SetBlockAsync(CommandContext ctx,
            [Description("Channel of which block status to change.")] DiscordChannel channel,
            [Description("Whether the user should be blocked.")] bool blocked,
            [RemainingText, Description("Reason why this user is blocked.")] string reason = null)
        {
            var cid = (long)channel.Id;
            var block = this.Database.BlockedEntities.SingleOrDefault(x => x.Id == cid && x.Kind == DatabaseEntityKind.Channel);
            if (blocked && block == null)
            {
                block = new DatabaseBlockedEntity
                {
                    Id = cid,
                    Kind = DatabaseEntityKind.Channel,
                    Reason = reason,
                    Since = DateTime.UtcNow
                };
                this.Database.BlockedEntities.Add(block);
            }
            else if (!blocked && block != null)
            {
                this.Database.BlockedEntities.Remove(block);
            }

            await this.Database.SaveChangesAsync().ConfigureAwait(false);
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Channel {channel.Mention} is {(blocked ? "now blocked" : "no longer blocked")}.").ConfigureAwait(false);
        }

        [Command("setblock")]
        public async Task SetBlockAsync(CommandContext ctx,
            [Description("Guild of which block status to change.")] DiscordGuild guild,
            [Description("Whether the user should be blocked.")] bool blocked,
            [RemainingText, Description("Reason why this user is blocked.")] string reason = null)
        {
            var gid = (long)guild.Id;
            var block = this.Database.BlockedEntities.SingleOrDefault(x => x.Id == gid && x.Kind == DatabaseEntityKind.Guild);
            if (blocked && block == null)
            {
                block = new DatabaseBlockedEntity
                {
                    Id = gid,
                    Kind = DatabaseEntityKind.Guild,
                    Reason = reason,
                    Since = DateTime.UtcNow
                };
                this.Database.BlockedEntities.Add(block);
            }
            else if (!blocked && block != null)
            {
                this.Database.BlockedEntities.Remove(block);
            }

            await this.Database.SaveChangesAsync().ConfigureAwait(false);
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Guild {Formatter.Bold(Formatter.Sanitize(guild.Name))} is {(blocked ? "now blocked" : "no longer blocked")}.").ConfigureAwait(false);
        }

        [Command("addprefix"), Description("Adds a prefix to this guild's command prefixes."), Aliases("addpfix")]
        public async Task AddPrefixAsync(CommandContext ctx,
            [RemainingText, Description("Prefix to add to this guild's prefixes.")] string prefix)
        {
            if (this.Bot.Configuration.Discord.DefaultPrefixes.Contains(prefix))
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} Cannot add default prefix.").ConfigureAwait(false);
                return;
            }

            var gid = (long)ctx.Guild.Id;
            var gpfix = this.Database.Prefixes.SingleOrDefault(x => x.GuildId == gid);
            if (gpfix == null)
            {
                gpfix = new DatabasePrefix
                {
                    GuildId = gid,
                    Prefixes = new[] { prefix },
                    EnableDefault = true
                };
                this.Database.Prefixes.Add(gpfix);
            }
            else if (!gpfix.Prefixes.Contains(prefix))
            {
                gpfix.Prefixes = gpfix.Prefixes.Concat(new[] { prefix }).ToArray();
                this.Database.Prefixes.Update(gpfix);
            }

            await this.Database.SaveChangesAsync().ConfigureAwait(false);
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Prefix added.").ConfigureAwait(false);
        }

        [Command("removeprefix"), Description("Removes a prefix from this guild's command prefixes."), Aliases("rmpfix")]
        public async Task RemovePrefixAsync(CommandContext ctx,
            [RemainingText, Description("Prefix to remove from this guild's prefixes.")] string prefix)
        {
            var gid = (long)ctx.Guild.Id;
            var gpfix = this.Database.Prefixes.SingleOrDefault(x => x.GuildId == gid);
            if (gpfix != null && gpfix.Prefixes.Contains(prefix))
            {
                gpfix.Prefixes = gpfix.Prefixes.Concat(new[] { prefix }).ToArray();
                this.Database.Prefixes.Update(gpfix);
            }
            else if (gpfix != null && !gpfix.Prefixes.Contains(prefix))
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} This prefix is not configured.").ConfigureAwait(false);
                return;
            }

            await this.Database.SaveChangesAsync().ConfigureAwait(false);
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Prefix removed.").ConfigureAwait(false);
        }

        [Command("configuredefaultprefixes"), Description("Configures whether default prefixes are to be enabled in this guild."), Aliases("cfgdefpfx")]
        public async Task ConfigureDefaultPrefixesAsync(CommandContext ctx,
            [RemainingText, Description("Whether default prefixes are to be enabled.")] bool enable)
        {
            var gid = (long)ctx.Guild.Id;
            var gpfix = this.Database.Prefixes.SingleOrDefault(x => x.GuildId == gid);
            if (gpfix == null)
            {
                gpfix = new DatabasePrefix
                {
                    GuildId = gid,
                    Prefixes = new string[] { },
                    EnableDefault = enable
                };
                this.Database.Prefixes.Add(gpfix);
            }
            else
            {
                gpfix.EnableDefault = enable;
                this.Database.Prefixes.Update(gpfix);
            }

            await this.Database.SaveChangesAsync().ConfigureAwait(false);
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Setting saved.").ConfigureAwait(false);
        }
    }

    public sealed class EvaluationEnvironment
    {
        public CommandContext Context { get; }

        public DiscordMessage Message => this.Context.Message;
        public DiscordChannel Channel => this.Context.Channel;
        public DiscordGuild Guild => this.Context.Guild;
        public DiscordUser User => this.Context.User;
        public DiscordMember Member => this.Context.Member;
        public DiscordClient Client => this.Context.Client;
        public HttpClient Http => this.Context.Services.GetService<HttpClient>();

        public EvaluationEnvironment(CommandContext ctx)
        {
            this.Context = ctx;
        }
    }
}