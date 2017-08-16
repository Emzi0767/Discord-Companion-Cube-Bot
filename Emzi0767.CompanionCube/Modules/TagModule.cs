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
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Emzi0767.CompanionCube.Services;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("tag", CanInvokeWithoutSubcommand = true), Description("Tag commands. Invoking without a subcommand will display requested tag."), NotBlocked]
    public sealed class TagModule
    {
        private static string[] ForbiddenNames { get; } = new[] { "create", "make", "delete", "remove", "force_delete", "force_remove", "edit", "modify", "force_edit", "force_modify", "history", "view_edit", "alias", "dump", "raw", "info", "approve", "unapprove", "list" };

        private DatabaseClient Database { get; }
        private SharedData Shared { get; }

        public TagModule(DatabaseClient database, SharedData shared)
        {
            this.Database = database;
            this.Shared = shared;
        }

        [Command("create"), Aliases("make"), Description("Creates a new tag.")]
        public async Task CreateAsync(CommandContext ctx, [Description("Name of the tag to create.")] string name, [RemainingText, Description("Contents of the tag to create.")] string contents)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));
            
            if (string.IsNullOrWhiteSpace(contents))
                throw new ArgumentException("Contents cannot be null, empty, or all-whitespace.", nameof(contents));

            if (contents.Length > 1500)
                throw new ArgumentException("Contents cannot be longer than 1500 characters.", nameof(contents));
            
            name = Formatter.Strip(name.ToLower()).Trim();
            var success = await this.Database.CreateTagAsync(ctx.User.Id, ctx.Channel.Id, name, contents).ConfigureAwait(false);
            var embed = new DiscordEmbedBuilder();
            if (success)
            {
                embed.Title = "Tag creation successful.";
                embed.Description = string.Concat("A channel tag named ", Formatter.InlineCode(name), " was created successfully.");
                embed.Color = new DiscordColor(0x007FFF);
            }
            else
            {
                embed.Title = "Tag creation failed.";
                embed.Description = string.Concat("Could not create tag named ", Formatter.Strip(name), ". It is possible that a tag with that name exists already.");
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("delete"), Aliases("remove"), Description("Deletes a tag.")]
        public async Task DeleteAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to delete.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));
            
            name = Formatter.Strip(name.ToLower()).Trim();
            var res = await this.Database.GetTagAsync(ctx.Channel.Id, name).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            if (res.IsSuccess)
            {
                var tag = res.ResultTag;

                var success = await this.Database.DeleteTagAsync(tag.Id, ctx.User.Id, false);
                if (!success)
                {
                    embed.Title = "Failed to delete tag";
                    embed.Description = "Make sure the tag exists and that you are its owner.";
                    embed.Color = new DiscordColor(0xFF0000);
                }
            }
            else
            {
                embed.Title = "Tag not found";
                if (res.SuggestedTags != null && res.SuggestedTags.Any())
                {
                    var sugs = string.Join(", ", res.SuggestedTags.Select(xt => Formatter.InlineCode(xt.Name)));
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found. Here are some suggestions:\n\n", sugs);
                }
                else
                {
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found.");
                }
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync(embed.Title == null ? DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString() : "", embed: embed.Title == null ? null : embed.Build()).ConfigureAwait(false);
        }

        [Command("force_delete"), Aliases("force_remove"), Description("Forcefully deletes a tag. This is meant for channel moderators."), OwnerOrPermission(Permissions.ManageChannels)]
        public async Task ForceDeleteAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to delete.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));
            
            name = Formatter.Strip(name.ToLower()).Trim();
            var res = await this.Database.GetTagAsync(ctx.Channel.Id, name).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            if (res.IsSuccess)
            {
                var tag = res.ResultTag;

                var success = await this.Database.DeleteTagAsync(tag.Id, ctx.User.Id, true);
                if (!success)
                {
                    embed.Title = "Failed to delete tag";
                    embed.Description = "Make sure the tag exists.";
                    embed.Color = new DiscordColor(0xFF0000);
                }
            }
            else
            {
                embed.Title = "Tag not found";
                if (res.SuggestedTags != null && res.SuggestedTags.Any())
                {
                    var sugs = string.Join(", ", res.SuggestedTags.Select(xt => Formatter.InlineCode(xt.Name)));
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found. Here are some suggestions:\n\n", sugs);
                }
                else
                {
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found.");
                }
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
            
            name = Formatter.Strip(name.ToLower()).Trim();
            var res = await this.Database.GetTagAsync(ctx.Channel.Id, name).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            if (res.IsSuccess)
            {
                var tag = res.ResultTag;

                var success = await this.Database.EditTagAsync(tag.Id, ctx.User.Id, new_contents, false).ConfigureAwait(false);
                if (!success)
                {
                    embed.Title = "Failed to edit tag";
                    embed.Description = "Make sure the tag exists and that you are its owner.";
                    embed.Color = new DiscordColor(0xFF0000);
                }
            }
            else
            {
                embed.Title = "Tag not found";
                if (res.SuggestedTags != null && res.SuggestedTags.Any())
                {
                    var sugs = string.Join(", ", res.SuggestedTags.Select(xt => Formatter.InlineCode(xt.Name)));
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found. Here are some suggestions:\n\n", sugs);
                }
                else
                {
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found.");
                }
                embed.Color = new DiscordColor(0xFF0000);
            }
            
            await ctx.RespondAsync(embed.Title == null ? DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString() : "", embed: embed.Title == null ? null : embed.Build()).ConfigureAwait(false);
        }

        [Command("force_edit"), Aliases("force_modify"), Description("Forcefully edits a tag. This is meant for channel moderators."), OwnerOrPermission(Permissions.ManageChannels)]
        public async Task ForceEditAsync(CommandContext ctx, [Description("Name of the tag to edit.")] string name, [RemainingText, Description("New contents of the tag,")] string new_contents)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));

            if (string.IsNullOrWhiteSpace(new_contents))
                throw new ArgumentException("Contents cannot be null, empty, or all-whitespace.", nameof(new_contents));

            if (new_contents.Length > 1500)
                throw new ArgumentException("Contents cannot be longer than 1500 characters.", nameof(new_contents));
            
            name = Formatter.Strip(name.ToLower()).Trim();
            var res = await this.Database.GetTagAsync(ctx.Channel.Id, name).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            if (res.IsSuccess)
            {
                var tag = res.ResultTag;

                var success = await this.Database.EditTagAsync(tag.Id, ctx.User.Id, new_contents, true).ConfigureAwait(false);
                if (!success)
                {
                    embed.Title = "Failed to edit tag";
                    embed.Description = "Make sure the tag exists and that you are its owner.";
                    embed.Color = new DiscordColor(0xFF0000);
                }
            }
            else
            {
                embed.Title = "Tag not found";
                if (res.SuggestedTags != null && res.SuggestedTags.Any())
                {
                    var sugs = string.Join(", ", res.SuggestedTags.Select(xt => Formatter.InlineCode(xt.Name)));
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found. Here are some suggestions:\n\n", sugs);
                }
                else
                {
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found.");
                }
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
            var res = await this.Database.GetTagAsync(ctx.Channel.Id, name).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            if (res.IsSuccess)
            {
                var tag = res.ResultTag;
                var sb = new StringBuilder();
                for (var i = 0; i < tag.Edits.Count; i++)
                {
                    sb.AppendFormat("`{0,-3}:` {1:yyyy-MM-dd HH:mm:ss.fff zzz} by <@!{2}>", i, tag.Edits[i], tag.EditingUserIds[i]).AppendLine();
                }

                embed.Title = string.Concat("List of edits to ", tag.Name);
                embed.Description = sb.ToString();
                embed.Color = new DiscordColor(0xD091B2);
            }
            else
            {
                embed.Title = "Tag not found";
                if (res.SuggestedTags != null && res.SuggestedTags.Any())
                {
                    var sugs = string.Join(", ", res.SuggestedTags.Select(xt => Formatter.InlineCode(xt.Name)));
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found. Here are some suggestions:\n\n", sugs);
                }
                else
                {
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found.");
                }
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("view_edit"), Description("Views a tag's specific edit.")]
        public async Task ViewEditAsync(CommandContext ctx, [Description("Tag to display historic edit for.")] string name, [Description("Historic edit ID.")] int edit_id)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));
            
            name = Formatter.Strip(name.ToLower()).Trim();
            var res = await this.Database.GetTagAsync(ctx.Channel.Id, name).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            var cntnt = "";
            if (res.IsSuccess)
            {
                var tag = res.ResultTag;

                if (ctx.User.Id != tag.OwnerId)
                    await this.Database.IncrementTagUsageAsync(tag.Id).ConfigureAwait(false);

                embed = null;
                cntnt = string.Concat("\u200b", tag.Contents[edit_id]);
            }
            else
            {
                embed.Title = "Tag not found";
                if (res.SuggestedTags != null && res.SuggestedTags.Any())
                {
                    var sugs = string.Join(", ", res.SuggestedTags.Select(xt => Formatter.InlineCode(xt.Name)));
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found. Here are some suggestions:\n\n", sugs);
                }
                else
                {
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found.");
                }
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
            
            name = Formatter.Strip(name.ToLower()).Trim();
            var res = await this.Database.GetTagAsync(ctx.Channel.Id, name).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            var cntnt = "";
            if (res.IsSuccess)
            {
                var tag = res.ResultTag;

                embed = null;
                cntnt = string.Concat("\u200b", Formatter.Sanitize(tag.Contents.Last()));
            }
            else
            {
                embed.Title = "Tag not found";
                if (res.SuggestedTags != null && res.SuggestedTags.Any())
                {
                    var sugs = string.Join(", ", res.SuggestedTags.Select(xt => Formatter.InlineCode(xt.Name)));
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found. Here are some suggestions:\n\n", sugs);
                }
                else
                {
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found.");
                }
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync(cntnt, embed: embed != null ? embed.Build() : null).ConfigureAwait(false);
        }

        [Command("info"), Description("Views information about a tag.")]
        public async Task InfoAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to view info of.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));
            
            name = Formatter.Strip(name.ToLower()).Trim();
            var res = await this.Database.GetTagAsync(ctx.Channel.Id, name).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            if (res.IsSuccess)
            {
                var tag = res.ResultTag;

                DiscordUser usr = ctx.Guild.Members.FirstOrDefault(xm => xm.Id == tag.OwnerId);
                if (usr == null)
                    usr = await ctx.Guild.GetMemberAsync(tag.OwnerId).ConfigureAwait(false);
                if (usr == null)
                    usr = await ctx.Client.GetUserAsync(tag.OwnerId).ConfigureAwait(false);

                embed.Title = tag.Name;
                embed.Color = new DiscordColor(0xD091B2);
                embed.Timestamp = tag.Edits.LastOrDefault();
                embed.WithFooter("Creation date");
                embed.WithAuthor(usr is DiscordMember mbr ? mbr.DisplayName : usr.Username, null, usr.AvatarUrl);
                embed.AddField("Originally created", tag.Edits.First().ToString("yyyy-MM-dd HH:mm:ss zzz"), false)
                    .AddField("Latest version from", tag.Edits.Last().ToString("yyyy-MM-dd HH:mm:ss zzz"), false)
                    .AddField("Version count", tag.Edits.Count.ToString("#,##0"), false)
                    .AddField("Use count", tag.Uses.ToString("#,##0"), false)
                    .AddField("Is hidden?", tag.IsHidden ? "Yes" : "No", false);
            }
            else
            {
                embed.Title = "Tag not found";
                if (res.SuggestedTags != null && res.SuggestedTags.Any())
                {
                    var sugs = string.Join(", ", res.SuggestedTags.Select(xt => Formatter.InlineCode(xt.Name)));
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found. Here are some suggestions:\n\n", sugs);
                }
                else
                {
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found.");
                }
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("approve"), Description("Approves a tag, making it appear in listings."), OwnerOrPermission(Permissions.ManageChannels)]
        public async Task ApproveAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to approve.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));
            
            name = Formatter.Strip(name.ToLower()).Trim();
            await this.Database.SetTagHiddenFlagAsync(ctx.Channel.Id, name, false).ConfigureAwait(false);
            await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
        }

        [Command("unapprove"), Description("Unapproves a tag, causing it to no longer be listed in tag listings."), OwnerOrPermission(Permissions.ManageChannels)]
        public async Task UnapproveAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to unapprove.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));
            
            name = Formatter.Strip(name.ToLower()).Trim();
            await this.Database.SetTagHiddenFlagAsync(ctx.Channel.Id, name, true).ConfigureAwait(false);
            await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
        }

        [Command("list"), Description("Lists tags, optionally specifying a search query.")]
        public async Task ListAsync(CommandContext ctx, [RemainingText, Description("Optional, tag name to search for.")] string like)
        {
            if (string.IsNullOrWhiteSpace(like) || ForbiddenNames.Contains(like.ToLower()))
                like = null;
            
            if (like != null)
                like = Formatter.Strip(like.ToLower()).Trim();
            var res = await this.Database.ListTagsAsync(ctx.Channel.Id, like).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            if (res.SuggestedTags != null && res.SuggestedTags.Any())
            {
                var tstr = string.Join(", ", res.SuggestedTags.Select(xt => Formatter.InlineCode(xt.Name)));

                embed.Title = "Tag list";
                embed.Description = string.Concat("Following tags matching your query were found:\n\n", tstr);
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

        public async Task ExecuteGroupAsync(CommandContext ctx, [RemainingText, Description("Name of the tag to display.")] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || ForbiddenNames.Contains(name.ToLower()))
                throw new ArgumentException("Name of the tag cannot be null, empty, all-whitespace, or equal to any of the tag command names.", nameof(name));
            
            name = Formatter.Strip(name.ToLower()).Trim();
            var res = await this.Database.GetTagAsync(ctx.Channel.Id, name).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            var cntnt = "";
            if (res.IsSuccess)
            {
                var tag = res.ResultTag;

                if (ctx.User.Id != tag.OwnerId)
                    await this.Database.IncrementTagUsageAsync(tag.Id).ConfigureAwait(false);

                embed = null;
                cntnt = string.Concat("\u200b", tag.Contents.Last());
            }
            else
            {
                embed.Title = "Tag not found";
                if (res.SuggestedTags != null && res.SuggestedTags.Any())
                {
                    var sugs = string.Join(", ", res.SuggestedTags.Select(xt => Formatter.InlineCode(xt.Name)));
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found. Here are some suggestions:\n\n", sugs);
                }
                else
                {
                    embed.Description = string.Concat("Tag with the name ", Formatter.InlineCode(name), " was not found.");
                }
                embed.Color = new DiscordColor(0xFF0000);
            }

            await ctx.RespondAsync(cntnt, embed: embed != null ? embed.Build() : null).ConfigureAwait(false);
        }
    }

    [NotBlocked]
    public sealed class TagsModule
    {
        [Command("tags"), Description("Lists tags, optionally specifying a search query.")]
        public async Task TagsAsync(CommandContext ctx, [RemainingText, Description("Optional, tag name to search for.")] string like)
        {
            var tag = ctx.CommandsNext.RegisteredCommands["tag"] as CommandGroup;
            var taglist = tag.Children.FirstOrDefault(xc => xc.Name == "list");

            await taglist.ExecuteAsync(ctx).ConfigureAwait(false);
        }
    }

    public struct Tag
    {
        public long Id { get; set; }

        public string Name { get; set; }
        public IReadOnlyList<string> Contents { get; set; }
        public IReadOnlyList<DateTimeOffset> Edits { get; set; }
        public IReadOnlyList<ulong> EditingUserIds { get; set; }

        public ulong OwnerId { get; set; }
        public ulong ChannelId { get; set; }

        public long Uses { get; set; }
        public bool IsHidden { get; set; }
    }

    public struct TagResult
    {
        public bool IsSuccess { get; set; }
        public Tag ResultTag { get; set; }
        public IReadOnlyList<Tag> SuggestedTags { get; set; }
    }
}