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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Emzi0767.CompanionCube.Attributes;
using Emzi0767.CompanionCube.Data;
using Emzi0767.CompanionCube.Services;
using Microsoft.EntityFrameworkCore;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("tag")]
    [Description("Tag commands. Invoking without a subcommand will display requested tag.")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [NotBlacklisted]
    [RequireGuild]
    public sealed class TagModule : BaseCommandModule
    {
        private static string[] ForbiddenNames { get; } = new[] { "create", "make", "delete", "remove", "force_delete", "force_remove", "edit", "modify", "force_edit", "force_modify", "history", "view_edit", "alias", "dump", "raw", "info", "unhide", "force_unhide", "hide", "force_hide", "list", "changetype", "force_changetype", "global", "@everyone", "@here" };

        private DatabaseContext Database { get; }

        public TagModule(DatabaseContext database)
        {
            this.Database = database;
        }

        [Command("create"), Aliases("make"), Description("Creates a new tag.")]
        public async Task CreateAsync(CommandContext ctx, 
            [Description("Type of the tag to create (guild/channel).")] TagType type,
            [Description("Name of the tag to create.")] string name, 
            [RemainingText, Description("Contents of the tag to create.")] string contents)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));
            
            if (string.IsNullOrWhiteSpace(contents))
                throw new ArgumentException("Contents cannot be null, empty, or all-whitespace.", nameof(contents));

            if (contents.Length > 1500)
                throw new ArgumentException("Contents cannot be longer than 1500 characters.", nameof(contents));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;

            var cnid = type == TagType.Channel ? cid : gid;
            var cnkind = type == TagType.Channel ? DatabaseTagKind.Channel : DatabaseTagKind.Guild;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = new DatabaseTag
            {
                ContainerId = cnid,
                Kind = cnkind,
                Name = name,
                IsHidden = false,
                OwnerId = (long)ctx.User.Id,
                LatestRevision = DateTime.UtcNow
            };
            var tagRev = new DatabaseTagRevision
            {
                ContainerId = tag.ContainerId,
                Kind = tag.Kind,
                Name = tag.Name,
                Contents = contents,
                CreatedAt = tag.LatestRevision,
                UserId = tag.OwnerId
            };

            if (this.Database.Tags.Any(x => x.ContainerId == cnid && x.Kind == cnkind && x.Name == name))
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} A {cnkind.ToString().ToLower()} tag with the name {Formatter.InlineCode(name)} already exists in this {cnkind.ToString().ToLower()}.");
                return;
            }

            await this.Database.Tags.AddAsync(tag);
            await this.Database.TagRevisions.AddAsync(tagRev);
            var modCount = await this.Database.SaveChangesAsync(false);
            if (modCount > 0)
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:"));
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not create the tag.");
        }

        [Command("delete"), Aliases("remove"), Description("Deletes a tag.")]
        public async Task DeleteAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to delete.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;
            var uid = (long)ctx.User.Id;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);
            
            if (tag != null)
            {
                this.Database.TagRevisions.RemoveRange(tag.Revisions);
                this.Database.Tags.Remove(tag);
                var modCount = await this.Database.SaveChangesAsync();
                if (modCount > 0)
                    await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:"));
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not delete the tag. Make sure the tag exists, you spelled the name correctly, and that you own it.");
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find the tag. Make sure you spelled the name correctly.");
        }

        [Command("force_delete"), Aliases("force_remove"), Description("Forcefully deletes a tag. This is meant for moderators."), OwnerOrPermission(Permissions.ManageChannels)]
        public async Task ForceDeleteAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to delete.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);
            
            if (tag != null)
            {
                this.Database.TagRevisions.RemoveRange(tag.Revisions);
                this.Database.Tags.Remove(tag);
                var modCount = await this.Database.SaveChangesAsync();
                if (modCount > 0)
                    await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:"));
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not delete the tag. Make sure the tag exists, and you spelled the name correctly.");
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find the tag. Make sure you spelled the name correctly.");
        }

        [Command("edit"), Aliases("modify"), Description("Edits a tag.")]
        public async Task EditAsync(CommandContext ctx, [Description("Name of the tag to edit.")] string name, [RemainingText, Description("New contents of the tag,")] string new_contents)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            if (string.IsNullOrWhiteSpace(new_contents))
                throw new ArgumentException("Contents cannot be null, empty, or all-whitespace.", nameof(new_contents));

            if (new_contents.Length > 1500)
                throw new ArgumentException("Contents cannot be longer than 1500 characters.", nameof(new_contents));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;
            var uid = (long)ctx.User.Id;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);
            
            if (tag != null)
            {
                var tagRev = new DatabaseTagRevision
                {
                    ContainerId = tag.ContainerId,
                    Kind = tag.Kind,
                    Name = tag.Name,
                    Contents = new_contents,
                    CreatedAt = DateTime.UtcNow,
                    UserId = uid
                };
                tag.LatestRevision = tagRev.CreatedAt;

                await this.Database.TagRevisions.AddAsync(tagRev);
                this.Database.Tags.Update(tag);
                var modCount = await this.Database.SaveChangesAsync();
                if (modCount > 0)
                    await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:"));
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not edit the tag. Make sure the tag exists, you spelled the name correctly, and that you own it.");
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find the tag. Make sure you spelled the name correctly.");
        }

        [Command("force_edit"), Aliases("force_modify"), Description("Forcefully edits a tag. This is meant for moderators."), OwnerOrPermission(Permissions.ManageChannels)]
        public async Task ForceEditAsync(CommandContext ctx, [Description("Name of the tag to edit.")] string name, [RemainingText, Description("New contents of the tag,")] string new_contents)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            if (string.IsNullOrWhiteSpace(new_contents))
                throw new ArgumentException("Contents cannot be null, empty, or all-whitespace.", nameof(new_contents));

            if (new_contents.Length > 1500)
                throw new ArgumentException("Contents cannot be longer than 1500 characters.", nameof(new_contents));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;
            var uid = (long)ctx.User.Id;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);
            
            if (tag != null)
            {
                var tagRev = new DatabaseTagRevision
                {
                    ContainerId = tag.ContainerId,
                    Kind = tag.Kind,
                    Name = tag.Name,
                    Contents = new_contents,
                    CreatedAt = DateTime.UtcNow,
                    UserId = uid
                };
                tag.LatestRevision = tagRev.CreatedAt;

                await this.Database.TagRevisions.AddAsync(tagRev);
                this.Database.Tags.Update(tag);
                var modCount = await this.Database.SaveChangesAsync();
                if (modCount > 0)
                    await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:"));
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not edit the tag. Make sure the tag exists, and you spelled the name correctly.");
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find the tag. Make sure you spelled the name correctly.");
        }

        [Command("history"), Description("Views edit history for a tag.")]
        public async Task HistoryAsync(CommandContext ctx, [RemainingText, Description("Tag to display edit history for.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);
            
            if (tag != null)
            {
                var users = await Task.WhenAll(tag.Revisions.Select(x => x.UserId).Distinct().Select(x => (ulong)x).Select(x => ctx.Client.GetUserAsync(x)));

                var sb = new StringBuilder()
                    .AppendLine($"List of edits to {Formatter.InlineCode(tag.Name)}")
                    .AppendLine();
                for (var i = 0; i < tag.Revisions.Count; i++)
                {
                    var tagRev = tag.Revisions.ElementAt(i);
                    var usr = users.FirstOrDefault(x => x.Id == (ulong)tagRev.UserId);
                    sb.AppendLine($"`{i,-3}:` {tagRev.CreatedAt:yyyy-MM-dd HH:mm:ss.fff zzz} by {Formatter.Bold(Formatter.Sanitize(usr.Username))}#{usr.Discriminator} ({usr.Id})");
                }

                await ctx.RespondAsync(sb.ToString().Replace("\r\n", "\n"));
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find the tag. Make sure you spelled the name correctly.");
        }

        [Command("view_edit"), Description("Views a tag's specific revision.")]
        public async Task ViewEditAsync(CommandContext ctx, [Description("Tag to display historic edit for.")] string name, [Description("Historic revision ID.")] int edit_id)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);
            
            if (tag != null)
            {
                if (edit_id >= tag.Revisions.Count || edit_id < 0)
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find specified tag revision. Make sure you entered a valid revision ID.");
                else
                    await ctx.RespondAsync($"\u200b{tag.Revisions.ElementAt(edit_id).Contents}");
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find the tag. Make sure you spelled the name correctly.");
        }

        // This might happen in the future, for now, disabled
        // [Command("alias"), Description("Creates an alias for a tag.")]
        // public async Task AliasAsync(CommandContext ctx, [Description("Name of the tag to alias.")] string target, [Description("Name of the alias.")] string alias)
        // {
        // 
        // }

        [Command("dump"), Aliases("raw"), Description("Dumps tag's contents in raw form.")]
        public async Task DumpAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to dump.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);
            
            if (tag != null)
            {
                var rev = tag.Revisions.First(x => x.CreatedAt == tag.LatestRevision);
                var cnt = rev.Contents.Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere");
                await ctx.RespondAsync($"\u200b{Formatter.Sanitize(cnt)}");
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find the tag. Make sure you spelled the name correctly.");
        }

        [Command("info"), Description("Views information about a tag.")]
        public async Task InfoAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to view info of.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);

            if (tag != null)
            {
                var uid = (ulong)tag.OwnerId;
                DiscordUser usr = null;
                try
                {
                    usr = await ctx.Guild.GetMemberAsync(uid);
                }
                catch (Exception)
                {
                    try
                    {
                        usr = await ctx.Client.GetUserAsync(uid);
                    }
                    catch (Exception)
                    { }
                }

                var embed = new DiscordEmbedBuilder
                {
                    Title = tag.Name,
                    Color = new DiscordColor(0xD091B2),
                    Timestamp = tag.LatestRevision,
                    Footer = new DiscordEmbedBuilder.EmbedFooter { Text = "Creation date" },
                    Author = new DiscordEmbedBuilder.EmbedAuthor { Name = usr is DiscordMember mbr ? mbr.DisplayName : (usr?.Username ?? "<unknown>"), IconUrl = usr?.AvatarUrl }
                }.AddField("Originally created", tag.Revisions.Min(x => x.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss zzz"), false)
                    .AddField("Latest version from", tag.Revisions.Max(x => x.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss zzz"), false)
                    .AddField("Kind", tag.Kind.ToString(), true)
                    .AddField("Version count", tag.Revisions.Count.ToString("#,##0"), true)
                    .AddField("Is hidden?", tag.IsHidden ? "Yes" : "No", true);
                await ctx.RespondAsync(embed: embed.Build());
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find the tag. Make sure you spelled the name correctly.");
        }

        [Command("unhide"), Description("Unhides a tag, which makes it appear in tag listings.")]
        public async Task UnhideAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to unhide.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));
            
            name = Formatter.Strip(name.ToLower()).Trim();
            var uid = (long)ctx.User.Id;
            var cid = (long)ctx.Channel.Id;
            var tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
            {
                var gid = (long)ctx.Guild.Id;
                tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);
            }

            if (tag != null)
            {
                if (tag.IsHidden)
                {
                    tag.IsHidden = false;
                    this.Database.Tags.Update(tag);
                    await this.Database.SaveChangesAsync();
                }
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString());
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not modify the tag. Make sure the tag exists, you spelled the name correctly, and that you own it.");
        }

        [Command("force_unhide"), Description("Forcefully unhides a tag. This is meant for moderators."), OwnerOrPermission(Permissions.ManageChannels)]
        public async Task ForceUnhideAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to unhide.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            name = Formatter.Strip(name.ToLower()).Trim();
            var cid = (long)ctx.Channel.Id;
            var tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
            {
                var gid = (long)ctx.Guild.Id;
                tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);
            }

            if (tag != null)
            {
                if (tag.IsHidden)
                {
                    tag.IsHidden = false;
                    this.Database.Tags.Update(tag);
                    await this.Database.SaveChangesAsync();
                }
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString());
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not modify the tag. Make sure the tag exists, and you spelled the name correctly.");
        }

        [Command("hide"), Description("Hides a tag, which prevents it from being displayed in tag listings.")]
        public async Task HideAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to hide.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));
            
            name = Formatter.Strip(name.ToLower()).Trim(); name = Formatter.Strip(name.ToLower()).Trim();
            var uid = (long)ctx.User.Id;
            var cid = (long)ctx.Channel.Id;
            var tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
            {
                var gid = (long)ctx.Guild.Id;
                tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);
            }

            if (tag != null)
            {
                if (!tag.IsHidden)
                {
                    tag.IsHidden = true;
                    this.Database.Tags.Update(tag);
                    await this.Database.SaveChangesAsync();
                }
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString());
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not modify the tag. Make sure the tag exists, you spelled the name correctly, and that you own it.");
        }

        [Command("force_hide"), Description("Forcefully hides a tag. This is meant for moderators."), OwnerOrPermission(Permissions.ManageChannels)]
        public async Task ForceHideAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to hide.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            name = Formatter.Strip(name.ToLower()).Trim(); name = Formatter.Strip(name.ToLower()).Trim();
            var cid = (long)ctx.Channel.Id;
            var tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
            {
                var gid = (long)ctx.Guild.Id;
                tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);
            }

            if (tag != null)
            {
                if (!tag.IsHidden)
                {
                    tag.IsHidden = true;
                    this.Database.Tags.Update(tag);
                    await this.Database.SaveChangesAsync();
                }
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString());
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not modify the tag. Make sure the tag exists, and you spelled the name correctly.");
        }

        [Command("changetype"), Description("Changes type of a tag between guild and channel.")]
        public async Task ChangeTypeAsync(CommandContext ctx, [Description("Name of the tag to change type for.")] string name, [Description("New type of the tag.")] TagType new_type)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            name = Formatter.Strip(name.ToLower()).Trim(); name = Formatter.Strip(name.ToLower()).Trim();
            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;
            var uid = (long)ctx.User.Id;
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);

            if (tag != null)
            {
                if ((new_type == TagType.Channel && tag.Kind == DatabaseTagKind.Guild) || tag.Kind == DatabaseTagKind.Channel)
                {
                    var ntag = new DatabaseTag
                    {
                        ContainerId = new_type == TagType.Channel ? cid : gid,
                        Kind = new_type == TagType.Channel ? DatabaseTagKind.Channel : DatabaseTagKind.Guild,
                        Name = tag.Name,
                        IsHidden = tag.IsHidden,
                        OwnerId = tag.OwnerId,
                        LatestRevision = tag.LatestRevision
                    };
                    var nrevs = tag.Revisions.Select(x => new DatabaseTagRevision
                    {
                        ContainerId = ntag.ContainerId,
                        Kind = ntag.Kind,
                        Name = ntag.Name,
                        Contents = x.Contents,
                        CreatedAt = x.CreatedAt,
                        UserId = x.UserId
                    }).ToList();

                    if (this.Database.Tags.Any(x => x.Kind == ntag.Kind && x.ContainerId == ntag.ContainerId && x.Name == ntag.Name))
                        await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not modify the tag, because a tag with target parameters already exists.");

                    this.Database.TagRevisions.RemoveRange(tag.Revisions);
                    await this.Database.SaveChangesAsync();
                    this.Database.Tags.Remove(tag);
                    await this.Database.Tags.AddAsync(ntag);
                    await this.Database.TagRevisions.AddRangeAsync(nrevs);
                    await this.Database.SaveChangesAsync();
                }
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString());
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not modify the tag. Make sure the tag exists, you spelled the name correctly, and that you own it.");
        }

        [Command("force_changetype"), Description("Forcibly changes type of a tag. This is meant for moderators."), OwnerOrPermission(Permissions.ManageMessages)]
        public async Task ForceChangeTypeAsync(CommandContext ctx, [Description("Name of the tag to change type for.")] string name, [Description("New type of the tag.")] TagType new_type)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            name = Formatter.Strip(name.ToLower()).Trim(); name = Formatter.Strip(name.ToLower()).Trim();
            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name&& x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name&& x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);

            if (tag != null && ((new_type == TagType.Channel && tag.Kind == DatabaseTagKind.Guild) || tag.Kind == DatabaseTagKind.Channel))
            {
                var ntag = new DatabaseTag
                {
                    ContainerId = new_type == TagType.Channel ? cid : gid,
                    Kind = new_type == TagType.Channel ? DatabaseTagKind.Channel : DatabaseTagKind.Guild,
                    Name = tag.Name,
                    IsHidden = tag.IsHidden,
                    OwnerId = tag.OwnerId,
                    LatestRevision = tag.LatestRevision
                };
                var nrevs = tag.Revisions.Select(x => new DatabaseTagRevision
                {
                    ContainerId = ntag.ContainerId,
                    Kind = ntag.Kind,
                    Name = ntag.Name,
                    Contents = x.Contents,
                    CreatedAt = x.CreatedAt,
                    UserId = x.UserId
                }).ToList();

                if (this.Database.Tags.Any(x => x.Kind == ntag.Kind && x.ContainerId == ntag.ContainerId && x.Name == ntag.Name))
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not modify the tag, because a tag with target parameters already exists.");

                this.Database.TagRevisions.RemoveRange(tag.Revisions);
                await this.Database.SaveChangesAsync();
                this.Database.Tags.Remove(tag);
                await this.Database.Tags.AddAsync(ntag);
                await this.Database.TagRevisions.AddRangeAsync(nrevs);
                await this.Database.SaveChangesAsync();

                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString());
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not modify the tag. Make sure the tag exists, and you spelled the name correctly.");
        }

        [Command("list"), Description("Lists tags, optionally specifying a search query.")]
        public async Task ListAsync(CommandContext ctx, [RemainingText, Description("Optional, tag name to search for.")] string like)
        {
            if (string.IsNullOrWhiteSpace(like) || ForbiddenNames.Contains(like.ToLower()))
                like = null;

            var gid = (long)ctx.Guild.Id;
            var cid = (long)ctx.Channel.Id;

            like = !string.IsNullOrWhiteSpace(like)
                ? Formatter.Strip(like.ToLower()).Trim()
                : null;

            var res = like == null ?
                this.Database.Tags.Where(x => ((x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid) || (x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid) || x.Kind == DatabaseTagKind.Global) && !x.IsHidden) :
                this.Database.Tags.FromSqlInterpolated($"select * from tags where ((kind = 'channel' and container_id = {cid}) or (kind = 'guild' and container_id = {gid}) or kind = 'global') and hidden = false and levenshtein_less_equal(name, {like}, 3) < 3");
            
            if (res.Any())
            {
                var tstr = string.Join(", ", res.OrderBy(x => x.Name).Select(xt => Formatter.InlineCode(xt.Name)).Distinct());

                await ctx.RespondAsync($"Following tags matching your query were found:\n\n{tstr}");
            }
            else
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} No tags were found.");
        }

        [GroupCommand]
        public async Task ExecuteGroupAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to display.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global);
            
            if (tag != null)
            {
                var rev = tag.Revisions.First(x => x.CreatedAt == tag.LatestRevision);
                var cnt = rev.Contents.Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere");
                await ctx.RespondAsync($"\u200b{cnt}");
            }
            else
            {
                var res = name != null
                    ? this.Database.Tags.Select(x => new { tag = x, similarity = EF.Functions.TrigramsSimilarity(x.Name, name) })
                    : this.Database.Tags.Select(x => new { tag = x, similarity = 1.0 });

                res = res.Where(x => (x.tag.Kind == DatabaseTagKind.Global || (x.tag.Kind == DatabaseTagKind.Guild && x.tag.ContainerId == gid) || (x.tag.Kind == DatabaseTagKind.Channel && x.tag.ContainerId == cid)) && !x.tag.IsHidden && x.similarity > 0.1)
                    .OrderByDescending(x => x.similarity)
                    .ThenBy(x => x.tag.Name);

                if (res.Any())
                { 
                    var sugs = string.Join(", ", res.Select(xt => Formatter.InlineCode(xt.tag.Name)).Distinct());
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Specified tag was not found. Here are some suggestions:\n\n{sugs}");
                }
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Specified tag was not found.");
            }
        }

        [Group("global")]
        [Description("Global tag management commands.")]
        [ModuleLifespan(ModuleLifespan.Transient)]
        public class GlobalTagModule : BaseCommandModule
        {
            private DatabaseContext Database { get; }

            public GlobalTagModule(DatabaseContext database)
            {
                this.Database = database;
            }

            [Command("create"), Aliases("make"), Description("Creates a new tag."), RequireOwner]
            public async Task CreateAsync(CommandContext ctx,
                [Description("Name of the tag to create.")] string name,
                [RemainingText, Description("Contents of the tag to create.")] string contents)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                if (string.IsNullOrWhiteSpace(contents))
                    throw new ArgumentException("Contents cannot be null, empty, or all-whitespace.", nameof(contents));

                if (contents.Length > 1500)
                    throw new ArgumentException("Contents cannot be longer than 1500 characters.", nameof(contents));

                name = Formatter.Strip(name.ToLower()).Trim();
                var tag = new DatabaseTag
                {
                    ContainerId = 0,
                    Kind = DatabaseTagKind.Global,
                    Name = name,
                    IsHidden = false,
                    OwnerId = (long)ctx.User.Id,
                    LatestRevision = DateTime.UtcNow
                };
                var tagRev = new DatabaseTagRevision
                {
                    ContainerId = tag.ContainerId,
                    Kind = tag.Kind,
                    Name = tag.Name,
                    Contents = contents,
                    CreatedAt = tag.LatestRevision,
                    UserId = tag.OwnerId
                };

                if (this.Database.Tags.Any(x => x.ContainerId == 0 && x.Kind == DatabaseTagKind.Global && x.Name == name))
                {
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} A global tag with the name {Formatter.InlineCode(name)} already exists.");
                    return;
                }

                await this.Database.Tags.AddAsync(tag);
                await this.Database.TagRevisions.AddAsync(tagRev);
                var modCount = await this.Database.SaveChangesAsync(false);
                if (modCount > 0)
                    await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:"));
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not create the tag.");
            }

            [Command("delete"), Aliases("remove"), Description("Deletes a tag."), RequireOwner]
            public async Task DeleteAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to delete.")] string name)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                var cid = (long)ctx.Channel.Id;
                var gid = (long)ctx.Guild.Id;
                var uid = (long)ctx.User.Id;

                name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global);
                
                if (tag != null)
                {
                    this.Database.TagRevisions.RemoveRange(tag.Revisions);
                    this.Database.Tags.Remove(tag);
                    var modCount = await this.Database.SaveChangesAsync();
                    if (modCount > 0)
                        await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:"));
                    else
                        await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not delete the tag. Make sure the tag exists, and you spelled the name correctly.");
                }
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find the tag. Make sure you spelled the name correctly.");
            }

            [Command("edit"), Aliases("modify"), Description("Edits a tag."), RequireOwner]
            public async Task EditAsync(CommandContext ctx, [Description("Name of the tag to edit.")] string name, [RemainingText, Description("New contents of the tag,")] string new_contents)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                if (string.IsNullOrWhiteSpace(new_contents))
                    throw new ArgumentException("Contents cannot be null, empty, or all-whitespace.", nameof(new_contents));

                if (new_contents.Length > 1500)
                    throw new ArgumentException("Contents cannot be longer than 1500 characters.", nameof(new_contents));
                
                var uid = (long)ctx.User.Id;

                name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global);
                
                if (tag != null)
                {
                    var tagRev = new DatabaseTagRevision
                    {
                        ContainerId = tag.ContainerId,
                        Kind = tag.Kind,
                        Name = tag.Name,
                        Contents = new_contents,
                        CreatedAt = DateTime.UtcNow,
                        UserId = uid
                    };
                    tag.LatestRevision = tagRev.CreatedAt;

                    await this.Database.TagRevisions.AddAsync(tagRev);
                    this.Database.Tags.Update(tag);
                    var modCount = await this.Database.SaveChangesAsync();
                    if (modCount > 0)
                        await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:"));
                    else
                        await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not edit the tag. Make sure the tag exists, you spelled the name correctly, and that you own it.");
                }
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find the tag. Make sure you spelled the name correctly.");
            }

            [Command("history"), Description("Views edit history for a tag.")]
            public async Task HistoryAsync(CommandContext ctx, [RemainingText, Description("Tag to display edit history for.")] string name)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global);
                
                if (tag != null)
                {
                    var users = await Task.WhenAll(tag.Revisions.Select(x => x.UserId).Distinct().Select(x => (ulong)x).Select(x => ctx.Client.GetUserAsync(x)));

                    var sb = new StringBuilder()
                        .AppendLine($"List of edits to {Formatter.InlineCode(tag.Name)}")
                        .AppendLine();
                    for (var i = 0; i < tag.Revisions.Count; i++)
                    {
                        var tagRev = tag.Revisions.ElementAt(i);
                        var usr = users.FirstOrDefault(x => x.Id == (ulong)tagRev.UserId);
                        sb.AppendLine($"`{i,-3}:` {tagRev.CreatedAt:yyyy-MM-dd HH:mm:ss.fff zzz} by {Formatter.Bold(Formatter.Sanitize(usr.Username))}#{usr.Discriminator} ({usr.Id})");
                    }

                    await ctx.RespondAsync(sb.ToString().Replace("\r\n", "\n"));
                }
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find the tag. Make sure you spelled the name correctly.");
            }

            [Command("view_edit"), Description("Views a tag's specific revision.")]
            public async Task ViewEditAsync(CommandContext ctx, [Description("Tag to display historic edit for.")] string name, [Description("Historic revision ID.")] int edit_id)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global);
                
                if (tag != null)
                {
                    if (edit_id >= tag.Revisions.Count || edit_id < 0)
                        await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find specified tag revision. Make sure you entered a valid revision ID.");
                    else
                        await ctx.RespondAsync($"\u200b{tag.Revisions.ElementAt(edit_id).Contents}");
                }
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find the tag. Make sure you spelled the name correctly.");
            }

            [Command("dump"), Aliases("raw"), Description("Dumps tag's contents in raw form.")]
            public async Task DumpAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to dump.")] string name)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global);

                if (tag != null)
                {
                    var rev = tag.Revisions.First(x => x.CreatedAt == tag.LatestRevision);
                    var cnt = rev.Contents.Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere");
                    await ctx.RespondAsync($"\u200b{Formatter.Sanitize(cnt)}");
                }
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find the tag. Make sure you spelled the name correctly.");
            }

            [Command("info"), Description("Views information about a tag.")]
            public async Task InfoAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to view info of.")] string name)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                var cid = (long)ctx.Channel.Id;
                var gid = (long)ctx.Guild.Id;

                name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global);
                
                if (tag != null)
                {
                    var uid = (ulong)tag.OwnerId;
                    DiscordUser usr = null;
                    try
                    {
                        usr = await ctx.Guild.GetMemberAsync(uid);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            usr = await ctx.Client.GetUserAsync(uid);
                        }
                        catch (Exception)
                        { }
                    }

                    var embed = new DiscordEmbedBuilder
                    {
                        Title = tag.Name,
                        Color = new DiscordColor(0xD091B2),
                        Timestamp = tag.LatestRevision,
                        Footer = new DiscordEmbedBuilder.EmbedFooter { Text = "Creation date" },
                        Author = new DiscordEmbedBuilder.EmbedAuthor { Name = usr is DiscordMember mbr ? mbr.DisplayName : (usr?.Username ?? "<unknown>"), IconUrl = usr?.AvatarUrl }
                    }.AddField("Originally created", tag.Revisions.Min(x => x.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss zzz"), false)
                        .AddField("Latest version from", tag.Revisions.Max(x => x.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss zzz"), false)
                        .AddField("Kind", tag.Kind.ToString(), true)
                        .AddField("Version count", tag.Revisions.Count.ToString("#,##0"), true)
                        .AddField("Is hidden?", tag.IsHidden ? "Yes" : "No", true);
                    await ctx.RespondAsync(embed: embed.Build());
                }
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not find the tag. Make sure you spelled the name correctly.");
            }

            [Command("unhide"), Description("Unhides a tag, which makes it appear in tag listings."), RequireOwner]
            public async Task UnhideAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to approve.")] string name)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                name = Formatter.Strip(name.ToLower()).Trim(); name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global);

                if (tag != null)
                {
                    if (tag.IsHidden)
                    {
                        tag.IsHidden = false;
                        this.Database.Tags.Update(tag);
                        await this.Database.SaveChangesAsync();
                    }
                    await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString());
                }
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not modify the tag. Make sure the tag exists, and you spelled the name correctly.");
            }

            [Command("hide"), Description("Hides a tag, which prevents it from being displayed in tag listings."), RequireOwner]
            public async Task HideAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to unapprove.")] string name)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                name = Formatter.Strip(name.ToLower()).Trim(); name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global);

                if (tag != null)
                {
                    if (!tag.IsHidden)
                    {
                        tag.IsHidden = true;
                        this.Database.Tags.Update(tag);
                        await this.Database.SaveChangesAsync();
                    }
                    await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString());
                }
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Could not modify the tag. Make sure the tag exists, and you spelled the name correctly.");
            }

            [Command("list"), Description("Lists tags, optionally specifying a search query.")]
            public async Task ListAsync(CommandContext ctx, [RemainingText, Description("Optional, tag name to search for.")] string like)
            {
                if (string.IsNullOrWhiteSpace(like) || ForbiddenNames.Contains(like.ToLower()))
                    like = null;

                like = !string.IsNullOrWhiteSpace(like)
                    ? Formatter.Strip(like.ToLower()).Trim()
                    : null;

                var res = like != null
                    ? this.Database.Tags.Select(x => new { tag = x, similarity = EF.Functions.TrigramsSimilarity(x.Name, like) })
                    : this.Database.Tags.Select(x => new { tag = x, similarity = 1.0 });

                res = res.Where(x => x.tag.Kind == DatabaseTagKind.Global && !x.tag.IsHidden && x.similarity > 0.1)
                    .OrderByDescending(x => x.similarity)
                    .ThenBy(x => x.tag.Name);

                if (res.Any())
                {
                    var tstr = string.Join(", ", res.Select(xt => Formatter.InlineCode(xt.tag.Name)).Distinct());

                    await ctx.RespondAsync($"Following tags matching your query were found:\n\n{tstr}");
                }
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} No tags were found.");
            }

            [GroupCommand]
            public async Task ExecuteGroupAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to display.")] string name)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                var cid = (long)ctx.Channel.Id;
                var gid = (long)ctx.Guild.Id;

                name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global);

                if (tag != null)
                {
                    var rev = tag.Revisions.First(x => x.CreatedAt == tag.LatestRevision);
                    var cnt = rev.Contents.Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere");
                    await ctx.RespondAsync($"\u200b{cnt}");
                }
                else
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Specified tag was not found.");
            }
        }
    }

    [NotBlacklisted, ModuleLifespan(ModuleLifespan.Transient)]
    public sealed class TagsModule : BaseCommandModule
    {
        [Command("tags"), Description("Lists tags, optionally specifying a search query.")]
        public async Task TagsAsync(CommandContext ctx, [RemainingText, Description("Optional, tag name to search for.")] string like)
        {
            var cmd = ctx.CommandsNext.FindCommand($"tag list", out _);
            var fctx = ctx.CommandsNext.CreateFakeContext(ctx.Member, ctx.Channel, ctx.Message.Content, ctx.Prefix, cmd, like);
            await ctx.CommandsNext.ExecuteCommandAsync(fctx);
        }
    }
}
