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
            await ctx.Message.RespondAsync(msg, mentions: Array.Empty<IMention>());
        }
    }
}
