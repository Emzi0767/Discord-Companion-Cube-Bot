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
using Npgsql;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("tag")]
    [Description("Tag commands. Invoking without a subcommand will display requested tag.")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [NotBlocked]
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
            [Description("Type of the tag to create.")] TagType type,
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
            
            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = new DatabaseTag
            {
                ContainerId = type == TagType.Channel ? cid : gid,
                Kind = type == TagType.Channel ? DatabaseTagKind.Channel : DatabaseTagKind.Guild,
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
            await this.Database.Tags.AddAsync(tag).ConfigureAwait(false);
            await this.Database.TagRevisions.AddAsync(tagRev).ConfigureAwait(false);
            await this.Database.SaveChangesAsync(false);

            var embed = new DiscordEmbedBuilder
            {
                Title = "Tag creation successful.",
                Description = $"A {(type == TagType.Channel ? "channel" : "guild")} tag named {Formatter.InlineCode(name)} was created successfully.",
                Color = new DiscordColor(0x007FFF)
            };

            await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
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
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            if (tag != null)
            {
                this.Database.TagRevisions.RemoveRange(tag.Revisions);
                this.Database.Tags.Remove(tag);
                var modCount = await this.Database.SaveChangesAsync().ConfigureAwait(false);
                if (modCount <= 0)
                {
                    embed.Title = "Failed to delete tag";
                    embed.Description = "Make sure the tag exists and that you are its owner.";
                    embed.Color = new DiscordColor(0xFF0000);
                }
            }
            else
            {
                embed.Title = "Tag not found";
                embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Verify that the tag exists and that you are its owner.";
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync(embed.Title == null ? DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString() : "", embed: embed.Title == null ? null : embed.Build()).ConfigureAwait(false);
        }

        [Command("force_delete"), Aliases("force_remove"), Description("Forcefully deletes a tag. This is meant for moderators."), OwnerOrPermission(Permissions.ManageChannels)]
        public async Task ForceDeleteAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to delete.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            if (tag != null)
            {
                this.Database.TagRevisions.RemoveRange(tag.Revisions);
                this.Database.Tags.Remove(tag);
                var modCount = await this.Database.SaveChangesAsync().ConfigureAwait(false);
                if (modCount <= 0)
                {
                    embed.Title = "Failed to delete tag";
                    embed.Description = "Make sure the tag exists.";
                    embed.Color = new DiscordColor(0xFF0000);
                }
            }
            else
            {
                embed.Title = "Tag not found";
                embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Verify that the tag exists.";
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync(embed.Title == null ? DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString() : "", embed: embed.Title == null ? null : embed.Build()).ConfigureAwait(false);
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
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
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

                await this.Database.TagRevisions.AddAsync(tagRev).ConfigureAwait(false);
                this.Database.Tags.Update(tag);
                var modCount = await this.Database.SaveChangesAsync().ConfigureAwait(false);
                if (modCount <= 0)
                {
                    embed.Title = "Failed to edit tag";
                    embed.Description = "Make sure the tag exists and that you are its owner.";
                    embed.Color = new DiscordColor(0xFF0000);
                }
            }
            else
            {
                embed.Title = "Tag not found";
                embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Verify that the tag exists and that you are its owner.";
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync(embed.Title == null ? DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString() : "", embed: embed.Title == null ? null : embed.Build()).ConfigureAwait(false);
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
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
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

                await this.Database.TagRevisions.AddAsync(tagRev).ConfigureAwait(false);
                this.Database.Tags.Update(tag);
                var modCount = await this.Database.SaveChangesAsync().ConfigureAwait(false);
                if (modCount <= 0)
                {
                    embed.Title = "Failed to edit tag";
                    embed.Description = "Make sure the tag exists.";
                    embed.Color = new DiscordColor(0xFF0000);
                }
            }
            else
            {
                embed.Title = "Tag not found";
                embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Verify that the tag exists.";
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync(embed.Title == null ? DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString() : "", embed: embed.Title == null ? null : embed.Build()).ConfigureAwait(false);
        }

        [Command("history"), Description("Views edit history for a tag.")]
        public async Task HistoryAsync(CommandContext ctx, [RemainingText, Description("Tag to display edit history for.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            if (tag != null)
            {
                var sb = new StringBuilder();
                for (var i = 0; i < tag.Revisions.Count; i++)
                {
                    var tagRev = tag.Revisions.ElementAt(i);
                    sb.AppendLine($"`{i,-3}:` {tagRev.CreatedAt:yyyy-MM-dd HH:mm:ss.fff zzz} by <@!{tagRev.UserId}>");
                }

                embed.Title = $"List of edits to {Formatter.InlineCode(tag.Name)}";
                embed.Description = sb.ToString();
                embed.Color = new DiscordColor(0xD091B2);
            }
            else
            {
                embed.Title = "Tag not found";
                embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Verify that the tag exists.";
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("view_edit"), Description("Views a tag's specific revision.")]
        public async Task ViewEditAsync(CommandContext ctx, [Description("Tag to display historic edit for.")] string name, [Description("Historic revision ID.")] int edit_id)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            var cntnt = "";
            if (tag != null)
            {
                if (edit_id >= tag.Revisions.Count || edit_id < 0)
                {
                    embed.Title = "Tag revision not found";
                    embed.Description = $"Revision {edit_id} for tag with the name {Formatter.InlineCode(name)} was not found. Verify that the revision number is valid.";
                    embed.Color = new DiscordColor(0xFF0000);
                }
                else
                {
                    embed = null;
                    cntnt = $"\u200b{tag.Revisions.ElementAt(edit_id).Contents}";
                }
            }
            else
            {
                embed.Title = "Tag not found";
                embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Verify that the tag exists.";
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync(cntnt, embed: embed != null ? embed.Build() : null).ConfigureAwait(false);
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
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            var cntnt = "";
            if (tag != null)
            {
                var rev = tag.Revisions.First(x => x.CreatedAt == tag.LatestRevision);
                var cnt = rev.Contents.Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere");
                cntnt = $"\u200b{Formatter.Sanitize(cnt)}";
            }
            else
            {
                embed.Title = "Tag not found";
                embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Verify that the tag exists.";
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync(cntnt.Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere"), embed: embed != null ? embed.Build() : null).ConfigureAwait(false);
        }

        [Command("info"), Description("Views information about a tag.")]
        public async Task InfoAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to view info of.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            if (tag != null)
            {
                var uid = (ulong)tag.OwnerId;
                var usr = await ctx.Guild.GetMemberAsync(uid).ConfigureAwait(false) ?? await ctx.Client.GetUserAsync(uid).ConfigureAwait(false);

                embed.Title = tag.Name;
                embed.Color = new DiscordColor(0xD091B2);
                embed.Timestamp = tag.LatestRevision;
                embed.Footer = new DiscordEmbedBuilder.EmbedFooter { Text = "Creation date" };
                embed.Author = new DiscordEmbedBuilder.EmbedAuthor { Name = usr is DiscordMember mbr ? mbr.DisplayName : usr.Username, IconUrl = usr.AvatarUrl };
                embed.AddField("Originally created", tag.Revisions.Min(x => x.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss zzz"), false)
                    .AddField("Latest version from", tag.Revisions.Max(x => x.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss zzz"), false)
                    .AddField("Kind", tag.Kind.ToString(), true)
                    .AddField("Version count", tag.Revisions.Count.ToString("#,##0"), true)
                    .AddField("Is hidden?", tag.IsHidden ? "Yes" : "No", true);
            }
            else
            {
                embed.Title = "Tag not found";
                embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Verify that the tag exists.";
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("unhide"), Description("Unhides a tag, which makes it appear in tag listings.")]
        public async Task UnhideAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to unhide.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));
            
            name = Formatter.Strip(name.ToLower()).Trim();
            var uid = (long)ctx.User.Id;
            var cid = (long)ctx.Channel.Id;
            var tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
            {
                var gid = (long)ctx.Guild.Id;
                tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);
            }

            if (tag != null && tag.IsHidden)
            {
                tag.IsHidden = false;
                this.Database.Tags.Update(tag);
                await this.Database.SaveChangesAsync().ConfigureAwait(false);
            }
            
            await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
        }

        [Command("force_unhide"), Description("Forcefully unhides a tag. This is meant for moderators."), OwnerOrPermission(Permissions.ManageChannels)]
        public async Task ForceUnhideAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to unhide.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            name = Formatter.Strip(name.ToLower()).Trim();
            var cid = (long)ctx.Channel.Id;
            var tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
            {
                var gid = (long)ctx.Guild.Id;
                tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);
            }

            if (tag != null && tag.IsHidden)
            {
                tag.IsHidden = false;
                this.Database.Tags.Update(tag);
                await this.Database.SaveChangesAsync().ConfigureAwait(false);
            }

            await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
        }

        [Command("hide"), Description("Hides a tag, which prevents it from being displayed in tag listings.")]
        public async Task HideAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to hide.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));
            
            name = Formatter.Strip(name.ToLower()).Trim(); name = Formatter.Strip(name.ToLower()).Trim();
            var uid = (long)ctx.User.Id;
            var cid = (long)ctx.Channel.Id;
            var tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
            {
                var gid = (long)ctx.Guild.Id;
                tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);
            }

            if (tag != null && !tag.IsHidden)
            {
                tag.IsHidden = true;
                this.Database.Tags.Update(tag);
                await this.Database.SaveChangesAsync().ConfigureAwait(false);
            }

            await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
        }

        [Command("force_hide"), Description("Forcefully hides a tag. This is meant for moderators."), OwnerOrPermission(Permissions.ManageChannels)]
        public async Task ForceHideAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to hide.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            name = Formatter.Strip(name.ToLower()).Trim(); name = Formatter.Strip(name.ToLower()).Trim();
            var cid = (long)ctx.Channel.Id;
            var tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
            {
                var gid = (long)ctx.Guild.Id;
                tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);
            }

            if (tag != null && !tag.IsHidden)
            {
                tag.IsHidden = true;
                this.Database.Tags.Update(tag);
                await this.Database.SaveChangesAsync().ConfigureAwait(false);
            }

            await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
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
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.OwnerId == uid && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);

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
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Tag not found",
                        Description = $"Tag with target parameters already exists.",
                        Color = new DiscordColor(0xFF0000)
                    };
                    await ctx.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
                    return;
                }

                this.Database.TagRevisions.RemoveRange(tag.Revisions);
                await this.Database.SaveChangesAsync();
                this.Database.Tags.Remove(tag);
                await this.Database.Tags.AddAsync(ntag).ConfigureAwait(false);
                await this.Database.TagRevisions.AddRangeAsync(nrevs).ConfigureAwait(false);
                await this.Database.SaveChangesAsync();
            }

            await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
        }

        [Command("force_changetype"), Description("Forcibly changes type of a tag. This is meant for moderators."), OwnerOrPermission(Permissions.ManageMessages)]
        public async Task ForceChangeTypeAsync(CommandContext ctx, [Description("Name of the tag to change type for.")] string name, [Description("New type of the tag.")] TagType new_type)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            name = Formatter.Strip(name.ToLower()).Trim(); name = Formatter.Strip(name.ToLower()).Trim();
            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name&& x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name&& x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);

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
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Tag not found",
                        Description = $"Tag with target parameters already exists.",
                        Color = new DiscordColor(0xFF0000)
                    };
                    await ctx.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
                    return;
                }

                this.Database.TagRevisions.RemoveRange(tag.Revisions);
                await this.Database.SaveChangesAsync();
                this.Database.Tags.Remove(tag);
                await this.Database.Tags.AddAsync(ntag).ConfigureAwait(false);
                await this.Database.TagRevisions.AddRangeAsync(nrevs).ConfigureAwait(false);
                await this.Database.SaveChangesAsync();
            }

            await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
        }

        [Command("list"), Description("Lists tags, optionally specifying a search query.")]
        public async Task ListAsync(CommandContext ctx, [RemainingText, Description("Optional, tag name to search for.")] string like)
        {
            if (string.IsNullOrWhiteSpace(like) || ForbiddenNames.Contains(like.ToLower()))
                like = null;

            var gid = (long)ctx.Guild.Id;
            var cid = (long)ctx.Channel.Id;

            if (!string.IsNullOrWhiteSpace(like))
                like = Formatter.Strip(like.ToLower()).Trim();
            else
                like = null;

            var res = like == null ?
                this.Database.Tags.Where(x => ((x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid) || (x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid) || x.Kind == DatabaseTagKind.Global) && !x.IsHidden) :
                this.Database.Tags.FromSql("select * from tags where ((kind = 'channel' and container_id = @cid) or (kind = 'guild' and container_id = @gid) or kind = 'global') and hidden = false and levenshtein_less_equal(name, @like, 3) < 3",
                new NpgsqlParameter("@cid", cid), new NpgsqlParameter("@gid", gid), new NpgsqlParameter("@like", like));

            var embed = new DiscordEmbedBuilder();
            if (res.Any())
            {
                var tstr = string.Join(", ", res.OrderBy(x => x.Name).Select(xt => Formatter.InlineCode(xt.Name)).Distinct());

                embed.Title = "Tag list";
                embed.Description = $"Following tags matching your query were found:\n\n{tstr}";
                embed.Color = new DiscordColor(0xD091B2);
            }
            else
            {
                embed.Title = "No tags found";
                embed.Description = "No tags matching the query were found.";
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        [GroupCommand]
        public async Task ExecuteGroupAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to display.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;

            name = Formatter.Strip(name.ToLower()).Trim();
            var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Channel && x.ContainerId == cid).ConfigureAwait(false);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Guild && x.ContainerId == gid).ConfigureAwait(false);
            if (tag == null)
                tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global).ConfigureAwait(false);

            DiscordEmbedBuilder embed = null;
            var cntnt = "";
            if (tag != null)
            {
                var rev = tag.Revisions.First(x => x.CreatedAt == tag.LatestRevision);
                var cnt = rev.Contents.Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere");
                cntnt = $"\u200b{cnt}";
            }
            else
            {
                var res = this.Database.Tags.FromSql("select * from tags where ((kind = 'channel' and container_id = @cid) or (kind = 'guild' and container_id = @gid) or kind = 'global') and hidden = false and levenshtein_less_equal(name, @like, 3) < 3",
                    new NpgsqlParameter("@cid", cid), new NpgsqlParameter("@gid", gid), new NpgsqlParameter("@like", name));

                embed = new DiscordEmbedBuilder
                {
                    Title = "Tag not found",
                    Color = new DiscordColor(0xFF0000)
                };

                if (res.Any())
                { 
                    var sugs = string.Join(", ", res.OrderBy(x => x.Name).Select(xt => Formatter.InlineCode(xt.Name)).Distinct());
                    embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Here are some suggestions:\n\n{sugs}";
                }
                else
                {
                    embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found.";
                }
            }

            await ctx.RespondAsync(cntnt, embed: embed != null ? embed.Build() : null).ConfigureAwait(false);
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
                await this.Database.Tags.AddAsync(tag).ConfigureAwait(false);
                await this.Database.TagRevisions.AddAsync(tagRev).ConfigureAwait(false);
                await this.Database.SaveChangesAsync(false);

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Tag creation successful.",
                    Description = $"A global tag named {Formatter.InlineCode(name)} was created successfully.",
                    Color = new DiscordColor(0x007FFF)
                };

                await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
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
                var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global).ConfigureAwait(false);

                var embed = new DiscordEmbedBuilder();
                if (tag != null)
                {
                    this.Database.TagRevisions.RemoveRange(tag.Revisions);
                    this.Database.Tags.Remove(tag);
                    var modCount = await this.Database.SaveChangesAsync().ConfigureAwait(false);
                    if (modCount <= 0)
                    {
                        embed.Title = "Failed to delete tag";
                        embed.Description = "Make sure the tag exists.";
                        embed.Color = new DiscordColor(0xFF0000);
                    }
                }
                else
                {
                    embed.Title = "Tag not found";
                    embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Verify that the tag exists.";
                    embed.Color = new DiscordColor(0xFF0000);
                }

                await ctx.RespondAsync(embed.Title == null ? DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString() : "", embed: embed.Title == null ? null : embed.Build()).ConfigureAwait(false);
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
                var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global).ConfigureAwait(false);

                var embed = new DiscordEmbedBuilder();
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

                    await this.Database.TagRevisions.AddAsync(tagRev).ConfigureAwait(false);
                    this.Database.Tags.Update(tag);
                    var modCount = await this.Database.SaveChangesAsync().ConfigureAwait(false);
                    if (modCount <= 0)
                    {
                        embed.Title = "Failed to edit tag";
                        embed.Description = "Make sure the tag exists.";
                        embed.Color = new DiscordColor(0xFF0000);
                    }
                }
                else
                {
                    embed.Title = "Tag not found";
                    embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Verify that the tag exists.";
                    embed.Color = new DiscordColor(0xFF0000);
                }

                await ctx.RespondAsync(embed.Title == null ? DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString() : "", embed: embed.Title == null ? null : embed.Build()).ConfigureAwait(false);
            }

            [Command("history"), Description("Views edit history for a tag.")]
            public async Task HistoryAsync(CommandContext ctx, [RemainingText, Description("Tag to display edit history for.")] string name)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global).ConfigureAwait(false);

                var embed = new DiscordEmbedBuilder();
                if (tag != null)
                {
                    var sb = new StringBuilder();
                    for (var i = 0; i < tag.Revisions.Count; i++)
                    {
                        var tagRev = tag.Revisions.ElementAt(i);
                        sb.AppendLine($"`{i,-3}:` {tagRev.CreatedAt:yyyy-MM-dd HH:mm:ss.fff zzz} by <@!{tagRev.UserId}>");
                    }

                    embed.Title = $"List of edits to {Formatter.InlineCode(tag.Name)}";
                    embed.Description = sb.ToString();
                    embed.Color = new DiscordColor(0xD091B2);
                }
                else
                {
                    embed.Title = "Tag not found";
                    embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Verify that the tag exists.";
                    embed.Color = new DiscordColor(0xFF0000);
                }

                await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
            }

            [Command("view_edit"), Description("Views a tag's specific revision.")]
            public async Task ViewEditAsync(CommandContext ctx, [Description("Tag to display historic edit for.")] string name, [Description("Historic revision ID.")] int edit_id)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global).ConfigureAwait(false);

                var embed = new DiscordEmbedBuilder();
                var cntnt = "";
                if (tag != null)
                {
                    if (edit_id >= tag.Revisions.Count || edit_id < 0)
                    {
                        embed.Title = "Tag revision not found";
                        embed.Description = $"Revision {edit_id} for tag with the name {Formatter.InlineCode(name)} was not found. Verify that the revision number is valid.";
                        embed.Color = new DiscordColor(0xFF0000);
                    }
                    else
                    {
                        embed = null;
                        cntnt = $"\u200b{tag.Revisions.ElementAt(edit_id).Contents}";
                    }
                }
                else
                {
                    embed.Title = "Tag not found";
                    embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Verify that the tag exists.";
                    embed.Color = new DiscordColor(0xFF0000);
                }

                await ctx.RespondAsync(cntnt, embed: embed != null ? embed.Build() : null).ConfigureAwait(false);
            }

            [Command("dump"), Aliases("raw"), Description("Dumps tag's contents in raw form.")]
            public async Task DumpAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to dump.")] string name)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global).ConfigureAwait(false);

                var embed = new DiscordEmbedBuilder();
                var cntnt = "";
                if (tag != null)
                {
                    var rev = tag.Revisions.First(x => x.CreatedAt == tag.LatestRevision);
                    var cnt = rev.Contents.Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere");
                    cntnt = $"\u200b{Formatter.Sanitize(cnt)}";
                }
                else
                {
                    embed.Title = "Tag not found";
                    embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Verify that the tag exists.";
                    embed.Color = new DiscordColor(0xFF0000);
                }

                await ctx.RespondAsync(cntnt.Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere"), embed: embed != null ? embed.Build() : null).ConfigureAwait(false);
            }

            [Command("info"), Description("Views information about a tag.")]
            public async Task InfoAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to view info of.")] string name)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                var cid = (long)ctx.Channel.Id;
                var gid = (long)ctx.Guild.Id;

                name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global).ConfigureAwait(false);

                var embed = new DiscordEmbedBuilder();
                if (tag != null)
                {
                    var uid = (ulong)tag.OwnerId;
                    var usr = await ctx.Guild.GetMemberAsync(uid).ConfigureAwait(false) ?? await ctx.Client.GetUserAsync(uid).ConfigureAwait(false);

                    embed.Title = tag.Name;
                    embed.Color = new DiscordColor(0xD091B2);
                    embed.Timestamp = tag.LatestRevision;
                    embed.Footer = new DiscordEmbedBuilder.EmbedFooter { Text = "Creation date" };
                    embed.Author = new DiscordEmbedBuilder.EmbedAuthor { Name = usr is DiscordMember mbr ? mbr.DisplayName : usr.Username, IconUrl = usr.AvatarUrl };
                    embed.AddField("Originally created", tag.Revisions.Min(x => x.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss zzz"), false)
                        .AddField("Latest version from", tag.Revisions.Max(x => x.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss zzz"), false)
                        .AddField("Kind", tag.Kind.ToString(), true)
                        .AddField("Version count", tag.Revisions.Count.ToString("#,##0"), true)
                        .AddField("Is hidden?", tag.IsHidden ? "Yes" : "No", true);
                }
                else
                {
                    embed.Title = "Tag not found";
                    embed.Description = $"Tag with the name {Formatter.InlineCode(name)} was not found. Verify that the tag exists.";
                    embed.Color = new DiscordColor(0xFF0000);
                }

                await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
            }

            [Command("unhide"), Description("Unhides a tag, which makes it appear in tag listings."), RequireOwner]
            public async Task UnhideAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to approve.")] string name)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                name = Formatter.Strip(name.ToLower()).Trim(); name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global).ConfigureAwait(false);

                if (tag != null && tag.IsHidden)
                {
                    tag.IsHidden = false;
                    this.Database.Tags.Update(tag);
                    await this.Database.SaveChangesAsync().ConfigureAwait(false);
                }

                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
            }

            [Command("hide"), Description("Hides a tag, which prevents it from being displayed in tag listings."), RequireOwner]
            public async Task HideAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to unapprove.")] string name)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                name = Formatter.Strip(name.ToLower()).Trim(); name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global).ConfigureAwait(false);

                if (tag != null && !tag.IsHidden)
                {
                    tag.IsHidden = true;
                    this.Database.Tags.Update(tag);
                    await this.Database.SaveChangesAsync().ConfigureAwait(false);
                }

                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
            }

            [Command("list"), Description("Lists tags, optionally specifying a search query.")]
            public async Task ListAsync(CommandContext ctx, [RemainingText, Description("Optional, tag name to search for.")] string like)
            {
                if (string.IsNullOrWhiteSpace(like) || ForbiddenNames.Contains(like.ToLower()))
                    like = null;

                if (!string.IsNullOrWhiteSpace(like))
                    like = Formatter.Strip(like.ToLower()).Trim();
                else
                    like = null;

                var res = like == null ?
                    this.Database.Tags.Where(x => x.Kind == DatabaseTagKind.Global && !x.IsHidden) :
                    this.Database.Tags.FromSql("select * from tags where kind = 'global' and hidden = false and levenshtein_less_equal(name, @like, 3) < 3",
                    new NpgsqlParameter("@like", like));

                var embed = new DiscordEmbedBuilder();
                if (res.Any())
                {
                    var tstr = string.Join(", ", res.OrderBy(x => x.Name).Select(xt => Formatter.InlineCode(xt.Name)).Distinct());

                    embed.Title = "Tag list";
                    embed.Description = $"Following tags matching your query were found:\n\n{tstr}";
                    embed.Color = new DiscordColor(0xD091B2);
                }
                else
                {
                    embed.Title = "No tags found";
                    embed.Description = "No tags matching the query were found.";
                    embed.Color = new DiscordColor(0xFF0000);
                }

                await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
            }

            [GroupCommand]
            public async Task ExecuteGroupAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to display.")] string name)
            {
                if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                    throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

                var cid = (long)ctx.Channel.Id;
                var gid = (long)ctx.Guild.Id;

                name = Formatter.Strip(name.ToLower()).Trim();
                var tag = await this.Database.Tags.Include(x => x.Revisions).SingleOrDefaultAsync(x => x.Name == name && x.Kind == DatabaseTagKind.Global).ConfigureAwait(false);

                DiscordEmbedBuilder embed = null;
                var cntnt = "";
                if (tag != null)
                {
                    var rev = tag.Revisions.First(x => x.CreatedAt == tag.LatestRevision);
                    var cnt = rev.Contents.Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere");
                    cntnt = $"\u200b{cnt}";
                }
                else
                {
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Tag not found",
                        Color = new DiscordColor(0xFF0000),
                        Description = $"Tag with the name {Formatter.InlineCode(name)} was not found."
                    };
                }

                await ctx.RespondAsync(cntnt, embed: embed != null ? embed.Build() : null).ConfigureAwait(false);
            }
        }
    }

    [NotBlocked, ModuleLifespan(ModuleLifespan.Transient)]
    public sealed class TagsModule : BaseCommandModule
    {
        [Command("tags"), Description("Lists tags, optionally specifying a search query.")]
        public async Task TagsAsync(CommandContext ctx, [RemainingText, Description("Optional, tag name to search for.")] string like)
        {
            var cmd = ctx.CommandsNext.FindCommand($"tag list", out _);
            var fctx = ctx.CommandsNext.CreateFakeContext(ctx.Member, ctx.Channel, ctx.Message.Content, ctx.Prefix, cmd, like);
            await ctx.CommandsNext.ExecuteCommandAsync(fctx).ConfigureAwait(false);
        }
    }
}