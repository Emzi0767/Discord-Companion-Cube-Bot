// This file is part of Companion Cube project
//
// Copyright (C) 2018-2021 Emzi0767
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

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

        public AdministrationModule(DatabaseContext database)
        {
            this.Database = database;
        }

        [Command("sudo"), Description("Executes a command as another user."), Hidden, RequireOwner, RequireGuild]
        public async Task SudoAsync(CommandContext ctx, [Description("Member to execute the command as.")] DiscordMember member, [RemainingText, Description("Command text to execute.")] string command)
        {
            var cmd = ctx.CommandsNext.FindCommand(command, out var args);
            if (cmd == null)
                throw new CommandNotFoundException(command);

            var fctx = ctx.CommandsNext.CreateFakeContext(member, ctx.Channel, command, ctx.Prefix, cmd, args);
            await ctx.CommandsNext.ExecuteCommandAsync(fctx);
        }

        [Command("sql"), Description("Executes a raw SQL query."), Hidden, RequireOwner]
        public async Task SqlQueryAsync(CommandContext ctx, [RemainingText, Description("SQL query to execute.")] string query)
        {
            var dat = new List<Dictionary<string, string>>();
            var i = 0;
            using (var cmd = this.Database.Database.GetDbConnection().CreateCommand())
            {
                await this.Database.Database.OpenConnectionAsync();

                cmd.CommandText = query;
                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    var dict = new Dictionary<string, string>();
                    for (i = 0; i < rdr.FieldCount; i++)
                        dict[rdr.GetName(i)] = rdr[i] is DBNull ? "<null>" : rdr[i].ToString();
                    dat.Add(dict);
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
                await ctx.RespondAsync("", embed: embed.Build());
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
            
            await ctx.RespondAsync("", embed: embed.Build());
        }

        [Command("eval"), Description("Evaluates a snippet of C# code, in context."), Hidden, RequireOwner]
        public async Task EvaluateAsync(CommandContext ctx, [RemainingText, Description("Code to evaluate.")] string code)
        {
            var cs1 = code.IndexOf("```") + 3;
            cs1 = code.IndexOf('\n', cs1) + 1;
            var cs2 = code.LastIndexOf("```");

            if (cs1 == -1 || cs2 == -1)
                throw new ArgumentException("You need to wrap the code into a code block.", nameof(code));

            code = code[cs1..cs2];

            var embed = new DiscordEmbedBuilder
            {
                Title = "Evaluating...",
                Color = new DiscordColor(0xD091B2)
            };
            var msg = await ctx.RespondAsync("", embed: embed.Build());

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
                await msg.ModifyAsync(embed: embed.Build());
                return;
            }

            Exception rex = null;
            ScriptState<object> css = null;
            var sw2 = Stopwatch.StartNew();
            try
            {
                css = await cs.RunAsync(globals);
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
                await msg.ModifyAsync(embed: embed.Build());
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

            await msg.ModifyAsync(embed: embed.Build());
        }

        [Command("nick"), Aliases("nickname"), Description("Changes the bot's nickname."), OwnerOrPermission(Permissions.ManageNicknames), RequireGuild]
        public async Task NicknameAsync(CommandContext ctx, [Description("New nickname for the bot.")] string new_nickname = "")
        {
            if (!ctx.Guild.Members.TryGetValue(ctx.Client.CurrentUser.Id, out var mbr))
                mbr = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);

            await mbr.ModifyAsync(x =>
            {
                x.Nickname = new_nickname;
                x.AuditLogReason = string.Concat("Edited by ", ctx.User.Username, "#", ctx.User.Discriminator, " (", ctx.User.Id, ")");
            });
            await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString());
        }

        [Command("musicwhitelist"), Description("Sets whether the specified guild should be whitelisted for music. Invoking with no arguments lists whitelisted guilds."), Aliases("musicwl"), RequireOwner]
        public async Task MusicAsync(CommandContext ctx,
            [Description("Guild for which to toggle the setting.")] DiscordGuild guild,
            [Description("Whether the music module should be available.")] bool whitelist,
            [RemainingText, Description("Reason why the guild has music enabled.")] string reason = null)
        {
            var gid = (long)guild.Id;
            var enable = this.Database.MusicWhitelist.SingleOrDefault(x => x.GuildId == gid);
            if (whitelist && enable == null)
            {
                enable = new DatabaseMusicWhitelistedGuild
                {
                    GuildId = gid,
                    Reason = reason
                };
                this.Database.MusicWhitelist.Add(enable);
            }
            else if (!whitelist && enable != null)
            {
                this.Database.MusicWhitelist.Remove(enable);
            }

            await this.Database.SaveChangesAsync();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} {Formatter.Bold(Formatter.Sanitize(guild.Name))} is {(whitelist ? "now whitelisted for music playback" : "not whitelisted for music playback anymore")}.");
        }

        [Command("musicwhitelist")]
        public async Task MusicAsync(CommandContext ctx)
        {
            var sb = new StringBuilder("Music is enabled in the following guilds (for this shard):\n\n");
            foreach (var x in this.Database.MusicWhitelist)
            {
                if (!ctx.Client.Guilds.TryGetValue((ulong)x.GuildId, out var gld))
                    continue;

                sb.Append($"{gld.Id} ({Formatter.Sanitize(gld.Name)}): {(string.IsNullOrWhiteSpace(x.Reason) ? "no reason specified" : x.Reason)}\n");
            }

            await ctx.RespondAsync(sb.ToString());
        }

        [Command("blacklist"), Description("Sets blacklisted status for a user, channel, or guild. Invoking with no arguments lists blacklisted entities."), Aliases("bl"), RequireOwner]
        public async Task BlacklistAsync(CommandContext ctx,
           [Description("User whose blacklisted status to change.")] DiscordUser user,
           [Description("Whether the user should be blacklisted.")] bool blacklisted,
           [RemainingText, Description("Reason why this user is blacklisted.")] string reason = null)
        {
            var uid = (long)user.Id;
            var block = this.Database.EntityBlacklist.SingleOrDefault(x => x.Id == uid && x.Kind == DatabaseEntityKind.User);
            if (blacklisted && block == null)
            {
                block = new DatabaseBlacklistedEntity
                {
                    Id = uid,
                    Kind = DatabaseEntityKind.User,
                    Reason = reason,
                    Since = DateTime.UtcNow
                };
                this.Database.EntityBlacklist.Add(block);
            }
            else if (!blacklisted && block != null)
            {
                this.Database.EntityBlacklist.Remove(block);
            }

            await this.Database.SaveChangesAsync();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} User {user.Mention} is {(blacklisted ? "now blacklisted" : "no longer blacklisted")}.");
        }

        [Command("blacklist")]
        public async Task BlacklistAsync(CommandContext ctx,
            [Description("Channel of which blacklisted status to change.")] DiscordChannel channel,
            [Description("Whether the user should be blacklisted.")] bool blacklisted,
            [RemainingText, Description("Reason why this user is blacklisted.")] string reason = null)
        {
            var cid = (long)channel.Id;
            var block = this.Database.EntityBlacklist.SingleOrDefault(x => x.Id == cid && x.Kind == DatabaseEntityKind.Channel);
            if (blacklisted && block == null)
            {
                block = new DatabaseBlacklistedEntity
                {
                    Id = cid,
                    Kind = DatabaseEntityKind.Channel,
                    Reason = reason,
                    Since = DateTime.UtcNow
                };
                this.Database.EntityBlacklist.Add(block);
            }
            else if (!blacklisted && block != null)
            {
                this.Database.EntityBlacklist.Remove(block);
            }

            await this.Database.SaveChangesAsync();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Channel {channel.Mention} is {(blacklisted ? "now blacklisted" : "no longer blacklisted")}.");
        }

        [Command("blacklist")]
        public async Task BlacklistAsync(CommandContext ctx,
            [Description("Guild of which blacklisted status to change.")] DiscordGuild guild,
            [Description("Whether the user should be blacklisted.")] bool blacklisted,
            [RemainingText, Description("Reason why this user is blacklisted.")] string reason = null)
        {
            var gid = (long)guild.Id;
            var block = this.Database.EntityBlacklist.SingleOrDefault(x => x.Id == gid && x.Kind == DatabaseEntityKind.Guild);
            if (blacklisted && block == null)
            {
                block = new DatabaseBlacklistedEntity
                {
                    Id = gid,
                    Kind = DatabaseEntityKind.Guild,
                    Reason = reason,
                    Since = DateTime.UtcNow
                };
                this.Database.EntityBlacklist.Add(block);
            }
            else if (!blacklisted && block != null)
            {
                this.Database.EntityBlacklist.Remove(block);
            }

            await this.Database.SaveChangesAsync();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Guild {Formatter.Bold(Formatter.Sanitize(guild.Name))} is {(blacklisted ? "now blacklisted" : "no longer blacklisted")}.");
        }

        [Command("blacklist")]
        public async Task BlacklistAsync(CommandContext ctx)
        {
            var sb = new StringBuilder("Following entities are blacklisted:\n\n");
            foreach (var x in this.Database.EntityBlacklist)
                sb.Append($"{(ulong)x.Id} ({x.Kind}, since {x.Since:yyyy-MM-dd HH:mm:ss zzz}): {(string.IsNullOrWhiteSpace(x.Reason) ? "no reason specified" : x.Reason)}\n");

            await ctx.RespondAsync(sb.ToString());
        }

        [Group("prefix"), ModuleLifespan(ModuleLifespan.Transient), Description("Commands for managing the prefixes that trigger the bot's commands."), Aliases("pfx")]
        public class Prefix : BaseCommandModule
        {
            private DatabaseContext Database { get; }
            private CompanionCubeBot Bot { get; }

            public Prefix(DatabaseContext database, CompanionCubeBot bot)
            {
                this.Database = database;
                this.Bot = bot;
            }

            [GroupCommand]
            public async Task ListAsync(CommandContext ctx)
            {
                var gid = (long)ctx.Guild.Id;
                var gpfx = this.Database.Prefixes.SingleOrDefault(x => x.GuildId == gid);
                var dcfg = this.Bot.Configuration.Discord;

                var sb = new StringBuilder();
                sb.Append($"Prefixes for {Formatter.Sanitize(ctx.Guild.Name)}:\n\n");
                if (gpfx == null)
                {
                    if (dcfg.DefaultPrefixes.Any())
                        sb.Append(string.Join(" ", dcfg.DefaultPrefixes.Select(Formatter.InlineCode)));

                    if (dcfg.DefaultPrefixes.Any() && dcfg.EnableMentionPrefix)
                        sb.Append(' ');

                    if (dcfg.EnableMentionPrefix)
                        sb.Append(ctx.Client.CurrentUser.Mention);
                }
                else
                {
                    if (dcfg.EnableMentionPrefix)
                        sb.Append(ctx.Client.CurrentUser.Mention);

                    if (dcfg.EnableMentionPrefix && gpfx.EnableDefault == true && dcfg.DefaultPrefixes.Any())
                        sb.Append(' ');

                    if (gpfx.EnableDefault == true && dcfg.DefaultPrefixes.Any())
                    {
                        sb.Append(string.Join(" ", dcfg.DefaultPrefixes.Select(Formatter.InlineCode)));
                    }

                    if (gpfx.EnableDefault == true && dcfg.DefaultPrefixes.Any() && gpfx.Prefixes?.Any() == true)
                        sb.Append(' ');

                    if (gpfx.Prefixes?.Any() == true)
                    {
                        sb.Append(string.Join(" ", gpfx.Prefixes.Select(Formatter.InlineCode)));
                    }
                }

                await ctx.RespondAsync(sb.ToString());
            }

            [Command("add"), Description("Adds a prefix to this guild's command prefixes."), RequireGuild]
            public async Task AddPrefixAsync(CommandContext ctx,
                [Description("Prefix to add to this guild's prefixes.")] string prefix)
            {
                var gid = (long)ctx.Guild.Id;
                var gpfx = this.Database.Prefixes.SingleOrDefault(x => x.GuildId == gid);

                if (gpfx?.EnableDefault != false && this.Bot.Configuration.Discord.DefaultPrefixes.Contains(prefix))
                {
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} Cannot add default prefix.");
                    return;
                }

                if (gpfx == null)
                {
                    gpfx = new DatabasePrefix
                    {
                        GuildId = gid,
                        Prefixes = new[] { prefix },
                        EnableDefault = true
                    };
                    this.Database.Prefixes.Add(gpfx);
                }
                else if (!gpfx.Prefixes.Contains(prefix))
                {
                    gpfx.Prefixes = gpfx.Prefixes.Concat(new[] { prefix }).ToArray();
                    this.Database.Prefixes.Update(gpfx);
                }

                await this.Database.SaveChangesAsync();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Prefix added.");
            }

            [Command("remove"), Description("Removes a prefix from this guild's command prefixes."), Aliases("rm", "delete", "del"), RequireGuild]
            public async Task RemovePrefixAsync(CommandContext ctx,
                [Description("Prefix to remove from this guild's prefixes.")] string prefix)
            {
                var gid = (long)ctx.Guild.Id;
                var gpfx = this.Database.Prefixes.SingleOrDefault(x => x.GuildId == gid);
                if (gpfx != null && gpfx.Prefixes.Contains(prefix))
                {
                    gpfx.Prefixes = gpfx.Prefixes.Except(new[] { prefix }).ToArray();
                    this.Database.Prefixes.Update(gpfx);
                }
                else if (gpfx != null && !gpfx.Prefixes.Contains(prefix))
                {
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} This prefix is not configured.");
                    return;
                }

                await this.Database.SaveChangesAsync();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Prefix removed.");
            }

            [Command("enabledefault"), Description("Configures whether default prefixes are to be enabled in this guild."), Aliases("default", "def"), RequireGuild]
            public async Task ConfigureDefaultPrefixesAsync(CommandContext ctx,
                [RemainingText, Description("Whether default prefixes are to be enabled.")] bool enable)
            {
                var gid = (long)ctx.Guild.Id;
                var gpfx = this.Database.Prefixes.SingleOrDefault(x => x.GuildId == gid);
                if (gpfx == null)
                {
                    gpfx = new DatabasePrefix
                    {
                        GuildId = gid,
                        Prefixes = Array.Empty<string>(),
                        EnableDefault = enable
                    };
                    this.Database.Prefixes.Add(gpfx);
                }
                else
                {
                    gpfx.EnableDefault = enable;
                    this.Database.Prefixes.Update(gpfx);
                }

                await this.Database.SaveChangesAsync();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Setting saved.");
            }
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
