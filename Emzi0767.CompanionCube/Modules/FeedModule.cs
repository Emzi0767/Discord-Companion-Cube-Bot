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
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Emzi0767.CompanionCube.Attributes;
using Emzi0767.CompanionCube.Services;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("rss")]
    [Aliases("syndication")]
    [Description("Commands for subscribing to syndication feeds.")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [NotBlacklisted]
    [RequirePermissions(Permissions.ManageChannels)]
    [RequireGuild]
    public sealed class FeedModule : BaseCommandModule
    {
        private FeedService Feeds { get; }

        public FeedModule(FeedService feeds)
        {
            this.Feeds = feeds;
        }

        [Command("add")]
        [Aliases("new", "subscribe", "sub")]
        [Description("Subscribes to an RSS feed, with new items being posted to specified channel.")]
        public async Task AddAsync(CommandContext ctx,
            [Description("Channel to post messages to.")] DiscordChannel channel,
            [Description("URL of the RSS or Atom feed to subscribe to.")] Uri url,
            [Description("Maximum number of items to replay when first scanning the feed.")] int? replay = null)
        {
            try
            {
                await this.Feeds.AddFeedAsync(url.Host, channel.Id, url, replay);
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:"));
            }
            catch
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Adding feed failed. Make sure that a feed with the same name or URL does not exist in that channel already.");
            }
        }

        [Command("add")]
        public async Task AddAsync(CommandContext ctx,
            [Description("Name for this feed.")] string name,
            [Description("Channel to post messages to.")] DiscordChannel channel,
            [Description("URL of the RSS or Atom feed to subscribe to.")] Uri url,
            [Description("Maximum number of items to replay when first scanning the feed.")] int? replay = null)
        {
            try
            {
                await this.Feeds.AddFeedAsync(name, channel.Id, url, replay);
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:"));
            }
            catch
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Adding feed failed. Make sure that a feed with the same name or URL does not exist in that channel already.");
            }
        }

        [Command("remove")]
        [Aliases("rm", "delete", "del", "unsubscribe", "unsub")]
        [Description("Unsubscribes from an RSS feed.")]
        public async Task RemoveAsync(CommandContext ctx,
            [Description("Name for this feed.")] string name,
            [Description("Channel in which messages are posted.")] DiscordChannel channel)
        {
            try
            {
                await this.Feeds.DeleteFeedAsync(name, channel.Id);
                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:"));
            }
            catch
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Removing feed failed. Make sure you specified the correct name and channel.");
            }
        }

        [Command("list")]
        [Aliases("get", "show")]
        [Description("Lists all feeds subscribed to a given channel.")]
        public async Task ListAsync(CommandContext ctx,
            [RemainingText, Description("Channel to list any attached feeds for.")] DiscordChannel channel)
        {
            if (channel == null)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} You did not specify a channel");
                return;
            }

            var feeds = await this.Feeds.GetFeedsAsync(channel.Id);
            if (!feeds.Any())
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Couldn't find any feeds");
                return;
            }

            var msg = string.Join("\n", feeds.Select(x => x.FormatFeed()));
            await ctx.RespondAsync(new DiscordMessageBuilder()
                .WithContent(msg)
                .WithAllowedMentions(Mentions.None));
        }
    }
}
