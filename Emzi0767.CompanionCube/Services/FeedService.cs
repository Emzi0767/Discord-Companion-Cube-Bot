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
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using DSharpPlus;
using DSharpPlus.Entities;
using Emzi0767.CompanionCube.Data;
using Microsoft.EntityFrameworkCore;

namespace Emzi0767.CompanionCube.Services
{
    public sealed class FeedService
    {
        private const string LinkRelContent = "alternate";

        private DiscordClient Discord { get; }
        private DatabaseContext Database { get; }
        private HttpClient Http { get; }

        public FeedService(
            DiscordClient discord,
            DatabaseContext database,
            HttpClient http)
        {
            this.Discord = discord;
            this.Database = database;
            this.Http = http;
        }

        public async Task ProcessFeedsAsync()
        {
            var feeds = await this.Database.RssFeeds.ToListAsync();
            var tasks = new List<Task>();
            foreach (var fgroup in feeds.GroupBy(x => x.Url))
            {
                SyndicationFeed rss;
                try
                {
                    rss = await this.GetItemsAsync(fgroup.Key);
                }
                catch
                {
                    rss = null;
                }

                if (rss == null)
                {
                    // Could be a temporary failure; skip processing
                    // TODO: Count failures, if they exceed a threshold, kill all feeds with this URL
                    continue;
                }

                var items = rss.Items.OrderByDescending(x => x.PublishDate).ToList();
                if (!items.Any())
                    continue;

                var latest = items.FirstOrDefault();
                var dto = latest.PublishDate;

                foreach (var feed in fgroup)
                {
                    int? replay = null;
                    if (feed.LastTimestamp == null)
                    {
                        replay = feed.InitializeReplayCount;
                        this.InitializeFeed(feed, dto);

                        if (replay == null)
                            continue;
                    }

                    var cid = (ulong)feed.ChannelId;
                    DiscordChannel chn;
                    try
                    {
                        chn = await this.Discord.GetChannelAsync(cid);
                    }
                    catch
                    {
                        chn = null;
                    }

                    if (chn == null)
                    {
                        this.RemoveDeadFeed(feed);
                        continue;
                    }

                    var last = feed.LastTimestamp.Value;
                    this.InitializeFeed(feed, dto);
                    var raws = replay == null 
                        ? items.Where(x => x.PublishDate > last)
                        : items.Take(replay.Value);
                    if (!raws.Any())
                        continue;

                    var posts = PrepareItems(raws.OrderBy(x => x.PublishDate));
                    tasks.AddRange(posts.Select(x => chn.SendMessageAsync(content: x.content, embed: x.embed)));
                }
            }

            await this.Database.SaveChangesAsync();
            await Task.WhenAll(tasks);
        }

        public async Task AddFeedAsync(string name, ulong channelId, Uri feedUrl, int? replay)
        {
            await this.Database.RssFeeds.AddAsync(new DatabaseRssFeed
            {
                Name = name,
                Url = feedUrl.ToString(),
                ChannelId = (long)channelId,
                InitializeReplayCount = replay
            });
            await this.Database.SaveChangesAsync();
        }

        public async Task DeleteFeedAsync(string name, ulong channelId)
        {
            var cid = (long)channelId;
            var feed = await this.Database.RssFeeds.FirstOrDefaultAsync(x => x.Name == name && x.ChannelId == cid);
            this.Database.RssFeeds.Remove(feed);
            await this.Database.SaveChangesAsync();
        }

        public async Task<IEnumerable<DatabaseRssFeed>> GetFeedsAsync(ulong channelId)
        {
            var cid = (long)channelId;
            return await this.Database.RssFeeds.Where(x => x.ChannelId == cid).ToListAsync();
        }

        private async Task<SyndicationFeed> GetItemsAsync(string url)
        {
            using var get = await this.Http.GetAsync(url);
            using var res = await get.Content.ReadAsStreamAsync();
            using var xml = XmlReader.Create(res);
            var rss = SyndicationFeed.Load(xml);

            return rss;
        }

        private void InitializeFeed(DatabaseRssFeed feed, DateTimeOffset dto)
        {
            feed.LastTimestamp = dto;
            feed.InitializeReplayCount = null;
            this.Database.RssFeeds.Update(feed);
        }

        private void RemoveDeadFeed(DatabaseRssFeed feed)
        {
            this.Database.RssFeeds.Remove(feed);
        }

        private static IEnumerable<(string content, DiscordEmbed embed)> PrepareItems(IEnumerable<SyndicationItem> items)
        {
            foreach (var item in items)
            {
                var uri = item.Links.FirstOrDefault(x => x.RelationshipType.Equals(LinkRelContent, StringComparison.InvariantCultureIgnoreCase))?.Uri
                    ?? item.Links.FirstOrDefault(x => x.Uri != null)?.Uri;

                if (uri == null)
                {
                    var embed = new DiscordEmbedBuilder()
                        .WithTitle(item.Title.Text)
                        .WithDescription(item.Summary?.Text.AtMost(100))
                        .WithTimestamp(item.PublishDate);

                    if (item.Authors.Count == 1)
                    {
                        var author = item.Authors.First();
                        embed.WithAuthor(author.Name);
                    }

                    yield return (null, embed.Build());
                }
                else
                {
                    yield return (uri.OriginalString, null);
                }
            }
        }
    }
}
