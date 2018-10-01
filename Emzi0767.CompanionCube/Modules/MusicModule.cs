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
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using Emzi0767.CompanionCube.Attributes;
using Emzi0767.CompanionCube.Data;
using Emzi0767.CompanionCube.Services;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("music")]
    [Description("Provides commands for music playback.")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [NotBlacklisted, MusicWhitelisted]
    public sealed class MusicModule : BaseCommandModule
    {
        private static ImmutableDictionary<int, DiscordEmoji> NumberMappings { get; }
        private static ImmutableDictionary<DiscordEmoji, int> NumberMappingsReverse { get; }
        private static ImmutableArray<DiscordEmoji> Numbers { get; }

        private MusicService Music { get; }
        private YouTubeSearchProvider YouTube { get; }

        private GuildMusicData GuildMusic { get; set; }

        public MusicModule(MusicService music, YouTubeSearchProvider yt)
        {
            this.Music = music;
            this.YouTube = yt;
        }

        static MusicModule()
        {
            var iab = ImmutableArray.CreateBuilder<DiscordEmoji>();
            iab.Add(DiscordEmoji.FromUnicode("1\u20e3"));
            iab.Add(DiscordEmoji.FromUnicode("2\u20e3"));
            iab.Add(DiscordEmoji.FromUnicode("3\u20e3"));
            iab.Add(DiscordEmoji.FromUnicode("4\u20e3"));
            iab.Add(DiscordEmoji.FromUnicode("5\u20e3"));
            iab.Add(DiscordEmoji.FromUnicode("\u274c"));
            Numbers = iab.ToImmutable();

            var idb = ImmutableDictionary.CreateBuilder<int, DiscordEmoji>();
            idb.Add(1, DiscordEmoji.FromUnicode("1\u20e3"));
            idb.Add(2, DiscordEmoji.FromUnicode("2\u20e3"));
            idb.Add(3, DiscordEmoji.FromUnicode("3\u20e3"));
            idb.Add(4, DiscordEmoji.FromUnicode("4\u20e3"));
            idb.Add(5, DiscordEmoji.FromUnicode("5\u20e3"));
            idb.Add(-1, DiscordEmoji.FromUnicode("\u274c"));
            NumberMappings = idb.ToImmutable();
            var idb2 = ImmutableDictionary.CreateBuilder<DiscordEmoji, int>();
            idb2.AddRange(NumberMappings.ToDictionary(x => x.Value, x => x.Key));
            NumberMappingsReverse = idb2.ToImmutable();
        }

        public override async Task BeforeExecutionAsync(CommandContext ctx)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} You need to be in a voice channel.").ConfigureAwait(false);
                throw new CommandCancelledException();
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} You need to be in the same voice channel.").ConfigureAwait(false);
                throw new CommandCancelledException();
            }

            this.GuildMusic = await this.Music.GetOrCreateDataAsync(ctx.Guild).ConfigureAwait(false);
            this.GuildMusic.CommandChannel = ctx.Channel;

            await base.BeforeExecutionAsync(ctx).ConfigureAwait(false);
        }

        [Command("play"), Description("Plays supplied URL or searches for specified keywords."), Aliases("p"), Priority(1)]
        public async Task PlayAsync(CommandContext ctx,
            [Description("URL to play from.")] Uri uri)
        {
            var trackLoad = await this.Music.GetTracksAsync(uri).ConfigureAwait(false);
            var tracks = trackLoad.Tracks;
            if (trackLoad.LoadResultType == LavalinkLoadResultType.LoadFailed || !tracks.Any())
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} No tracks were found at specified link.").ConfigureAwait(false);
                return;
            }

            if (this.GuildMusic.IsShuffled)
                tracks = this.Music.Shuffle(tracks);
            var trackCount = tracks.Count();
            foreach (var track in tracks)
                this.GuildMusic.Enqueue(new MusicItem(track, ctx.Member));

            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            await this.GuildMusic.CreatePlayerAsync(chn).ConfigureAwait(false);
            this.GuildMusic.Play();

            if (trackCount > 1)
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Added {trackCount:#,##0} tracks to playback queue.").ConfigureAwait(false);
            else
            {
                var track = tracks.First();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Added {Formatter.Bold(Formatter.Sanitize(track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Author))} to the playback queue.").ConfigureAwait(false);
            }
        }

        [Command("play"), Priority(0)]
        public async Task PlayAsync(CommandContext ctx,
            [RemainingText, Description("Terms to search for.")] string term)
        {
            var interactivity = ctx.Client.GetInteractivity();

            var results = await this.YouTube.SearchAsync(term).ConfigureAwait(false);
            if (!results.Any())
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Nothing was found.").ConfigureAwait(false);
                return;
            }

            var msgC = string.Join("\n", results.Select((x, i) => $"{NumberMappings[i + 1]} {Formatter.Bold(Formatter.Sanitize(x.Title))} by {Formatter.Bold(Formatter.Sanitize(x.Author))}"));
            msgC = $"{msgC}\n\nType a number 1-{results.Count()} to queue a track. To cancel, type cancel or {Numbers.Last()}.";
            var msg = await ctx.RespondAsync(msgC).ConfigureAwait(false);

            //foreach (var emoji in Numbers)
            //    await msg.CreateReactionAsync(emoji).ConfigureAwait(false);
            //var res = await interactivity.WaitForMessageReactionAsync(x => NumberMappingsReverse.ContainsKey(x), msg, ctx.User, TimeSpan.FromSeconds(30)).ConfigureAwait(false);

            var res = await interactivity.WaitForMessageAsync(x => x.Author == ctx.User, TimeSpan.FromSeconds(30));
            if (res == null)
            {
                await msg.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} No choice was made.").ConfigureAwait(false);
                return;
            }

            var resInd = res.Message.Content.Trim();
            if (!int.TryParse(resInd, NumberStyles.Integer, CultureInfo.InvariantCulture, out var elInd))
            {
                if (resInd.ToLowerInvariant() == "cancel")
                {
                    elInd = -1;
                }
                else
                {
                    var em = DiscordEmoji.FromUnicode(resInd);
                    if (!NumberMappingsReverse.ContainsKey(em))
                    {
                        await msg.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} Invalid choice was made.").ConfigureAwait(false);
                        return;
                    }

                    elInd = NumberMappingsReverse[em];
                }
            }
            else if (elInd < 1)
            {
                await msg.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} Invalid choice was made.").ConfigureAwait(false);
                return;
            }

            if (!NumberMappings.ContainsKey(elInd))
            {
                await msg.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} Invalid choice was made.").ConfigureAwait(false);
                return;
            }

            //var elInd = NumberMappingsReverse[res.Emoji];
            if (elInd == -1)
            {
                await msg.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Choice cancelled.").ConfigureAwait(false);
                return;
            }

            var el = results.ElementAt(elInd - 1);
            var url = new Uri($"https://youtu.be/{el.Id}");

            var trackLoad = await this.Music.GetTracksAsync(url).ConfigureAwait(false);
            var tracks = trackLoad.Tracks;
            if (trackLoad.LoadResultType == LavalinkLoadResultType.LoadFailed || !tracks.Any())
            {
                await msg.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} No tracks were found at specified link.").ConfigureAwait(false);
                return;
            }

            if (this.GuildMusic.IsShuffled)
                tracks = this.Music.Shuffle(tracks);
            var trackCount = tracks.Count();
            foreach (var track in tracks)
                this.GuildMusic.Enqueue(new MusicItem(track, ctx.Member));

            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            await this.GuildMusic.CreatePlayerAsync(chn).ConfigureAwait(false);
            this.GuildMusic.Play();

            if (trackCount > 1)
            {
                await msg.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Added {trackCount:#,##0} tracks to playback queue.").ConfigureAwait(false);
            }
            else
            {
                var track = tracks.First();
                await msg.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Added {Formatter.Bold(Formatter.Sanitize(track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Author))} to the playback queue.").ConfigureAwait(false);
            }
        }

        [Command("stop"), Description("Stops playback and quits the voice channel.")]
        public async Task StopAsync(CommandContext ctx)
        {
            int rmd = this.GuildMusic.EmptyQueue();
            this.GuildMusic.Stop();
            await this.GuildMusic.DestroyPlayerAsync().ConfigureAwait(false);

            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Removed {rmd:#,##0} tracks from the queue.").ConfigureAwait(false);
        }

        [Command("pause"), Description("Pauses playback.")]
        public async Task PauseAsync(CommandContext ctx)
        {
            this.GuildMusic.Pause();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Playback paused. Use {Formatter.InlineCode($"{ctx.Prefix}resume")} to resume playback.").ConfigureAwait(false);
        }

        [Command("resume"), Description("Resumes playback."), Aliases("unpause")]
        public async Task ResumeAsync(CommandContext ctx)
        {
            this.GuildMusic.Resume();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Playback resumed.").ConfigureAwait(false);
        }

        [Command("skip"), Description("Skips current track."), Aliases("next")]
        public async Task SkipAsync(CommandContext ctx)
        {
            var track = this.GuildMusic.NowPlaying;
            this.GuildMusic.Stop();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} {Formatter.Bold(Formatter.Sanitize(track.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Track.Author))} skipped.").ConfigureAwait(false);
        }

        [Command("seek"), Description("Seeks to specified time in current track.")]
        public Task SeekAsync(CommandContext ctx,
            [RemainingText, Description("Which time point to seek to.")] TimeSpan position)
        {
            this.GuildMusic.Seek(position, false);
            return Task.CompletedTask;
        }

        [Command("forward"), Description("Forwards the track by specified amount of time.")]
        public Task ForwardAsync(CommandContext ctx,
            [RemainingText, Description("By how much to forward.")] TimeSpan offset)
        {
            this.GuildMusic.Seek(offset, true);
            return Task.CompletedTask;
        }

        [Command("rewind"), Description("Rewinds the track by specified amount of time.")]
        public Task RewindAsync(CommandContext ctx,
            [RemainingText, Description("By how much to rewind.")] TimeSpan offset)
        {
            this.GuildMusic.Seek(-offset, true);
            return Task.CompletedTask;
        }

        [Command("volume"), Description("Sets playback volume."), Aliases("v")]
        public async Task VolumeAsync(CommandContext ctx,
            [Description("Volume to set. Can be 0-150. Default 100.")] int volume = 100)
        {
            if (volume < 0 || volume > 150)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} Volume must be greater than 0, and less than or equal to 150.").ConfigureAwait(false);
                return;
            }

            this.GuildMusic.SetVolume(volume);
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Volume set to {volume}%.").ConfigureAwait(false);
        }

        [Command("restart"), Description("Restarts the playback of the current track.")]
        public async Task RestartAsync(CommandContext ctx)
        {
            var track = this.GuildMusic.NowPlaying;
            this.GuildMusic.Restart();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} {Formatter.Bold(Formatter.Sanitize(track.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Track.Author))} restarted.").ConfigureAwait(false);
        }

        [Command("repeat"), Description("Changes repeat mode of the queue."), Aliases("loop")]
        public async Task RepeatAsync(CommandContext ctx,
            [Description("Repeat mode. Can be all, single, or none.")] string mode = null)
        {
            var rmc = new RepeatModeConverter();
            if (!rmc.TryFromString(mode, out var rm))
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} Invalid repeat mode specified.").ConfigureAwait(false);
                return;
            }

            this.GuildMusic.SetRepeatMode(rm);
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Repeat mode set to {rm}.").ConfigureAwait(false);
        }

        [Command("shuffle"), Description("Toggles shuffle mode.")]
        public async Task ShuffleAsync(CommandContext ctx)
        {
            if (this.GuildMusic.IsShuffled)
            {
                this.GuildMusic.StopShuffle();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Queue is no longer shuffled.").ConfigureAwait(false);
            }
            else
            {
                this.GuildMusic.Shuffle();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Queue is now shuffled.").ConfigureAwait(false);
            }
        }

        [Command("reshuffle"), Description("Reshuffles the queue. If queue is not shuffled, it won't enable shuffle mode.")]
        public async Task ReshuffleAsync(CommandContext ctx)
        {
            this.GuildMusic.Reshuffle();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Queue reshuffled.").ConfigureAwait(false);
        }

        [Command("remove"), Description("Removes a track from playback queue."), Aliases("del", "rm")]
        public async Task RemoveAsync(CommandContext ctx,
            [Description("Which track to remove.")] int index)
        {
            var itemN = this.GuildMusic.Remove(index - 1);
            if (itemN == null)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} No such track.").ConfigureAwait(false);
                return;
            }

            var track = itemN.Value;
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} {Formatter.Bold(Formatter.Sanitize(track.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Track.Author))} removed.").ConfigureAwait(false);
        }

        [Command("queue"), Description("Displays current playback queue."), Aliases("q")]
        public async Task QueueAsync(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivity();

            if (this.GuildMusic.RepeatMode == RepeatMode.Single)
            {
                var track = this.GuildMusic.NowPlaying;
                await ctx.RespondAsync($"Queue repeats {Formatter.Bold(Formatter.Sanitize(track.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Track.Author))}.").ConfigureAwait(false);
                return;
            }

            var pageCount = this.GuildMusic.Queue.Count / 10 + 1;
            if (this.GuildMusic.Queue.Count % 10 == 0) pageCount--;
            var pages = this.GuildMusic.Queue.Select(x => x.ToTrackString())
                .Select((s, i) => new { str = s, index = i })
                .GroupBy(x => x.index / 10)
                .Select(xg => new Page { Content = $"Now playing: {this.GuildMusic.NowPlaying.ToTrackString()}\n\n{string.Join("\n", xg.Select(xa => $"`{xa.index + 1:00}` {xa.str}"))}\n\n{(this.GuildMusic.RepeatMode == RepeatMode.All ? "The entire queue is repeated.\n\n" : "")}Page {xg.Key + 1}/{pageCount}" });

            var trk = this.GuildMusic.NowPlaying;
            if (!pages.Any() && trk.Track.TrackString == null)
                pages = new List<Page>() { new Page { Content = "Queue is empty!" } };
            else if (!pages.Any())
                pages = new List<Page>() { new Page { Content = $"Now playing: {this.GuildMusic.NowPlaying.ToTrackString()}" } };

            await interactivity.SendPaginatedMessage(ctx.Channel, ctx.User, pages, TimeSpan.FromMinutes(2), TimeoutBehaviour.Ignore);
        }

        [Command("nowplaying"), Description("Displays information about currently-played track."), Aliases("np")]
        public async Task NowPlayingAsync(CommandContext ctx)
        {
            var track = this.GuildMusic.NowPlaying;
            if (this.GuildMusic.NowPlaying.Track.TrackString == null)
            {
                await ctx.RespondAsync($"Not playing.").ConfigureAwait(false);
            }
            else
            {
                await ctx.RespondAsync($"Now playing: {Formatter.Bold(Formatter.Sanitize(track.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Track.Author))} [{this.GuildMusic.GetCurrentPosition().ToDurationString()}/{this.GuildMusic.NowPlaying.Track.Length.ToDurationString()}] requested by {Formatter.Bold(Formatter.Sanitize(this.GuildMusic.NowPlaying.RequestedBy.DisplayName))}.").ConfigureAwait(false);
            }
        }

        [Command("playerinfo"), Description("Displays information about current player."), Aliases("pinfo", "pinf"), Hidden]
        public async Task PlayerInfoAsync(CommandContext ctx)
        {
            await ctx.RespondAsync($"Queue length: {this.GuildMusic.Queue.Count}\nIs shuffled? {(this.GuildMusic.IsShuffled ? "Yes" : "No")}\nRepeat mode: {this.GuildMusic.RepeatMode}\nVolume: {this.GuildMusic.Volume}%").ConfigureAwait(false);
        }
    }
}
