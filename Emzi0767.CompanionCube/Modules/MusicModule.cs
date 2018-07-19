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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using Emzi0767.CompanionCube.Services;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("music"), Description("Provides commands for music playback."), ModuleLifespan(ModuleLifespan.Singleton), NotBlocked, MusicEnabled]
    public class MusicModule : BaseCommandModule
    {
        public LavalinkNodeConnection Lavalink { get; set; }
        public SharedData Shared { get; }
        public ConcurrentDictionary<ulong, GuildMusicData> MusicQueues { get; }
        public CSPRNG RNG { get; }
        public InteractivityExtension Interactivity { get; }

        public MusicModule(SharedData shared, CSPRNG rng)
        {
            this.Shared = shared;
            this.MusicQueues = new ConcurrentDictionary<ulong, GuildMusicData>();
            this.RNG = rng;
        }

        [Command("play"), Description("Joins the user's voice channel and plays music.")]
        public async Task PlayAsync(CommandContext ctx, [RemainingText, Description("URL to play music from.")] string source)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];

            if (source.StartsWith('<') && source.EndsWith('>'))
                source = source.Substring(1, source.Length - 2);
            var tracks = await this.Lavalink.GetTracksAsync(new Uri(source));
            if (!tracks.Any())
            {
                await ctx.RespondAsync("No tracks were found at specified link!").ConfigureAwait(false);
                return;
            }

            var trackCount = tracks.Count();
            foreach (var track in tracks)
                gmd.Enqueue(new MusicItem(track, ctx.Member));

            if (gmd.Player == null)
            {
                gmd.Player = await this.Lavalink.ConnectAsync(chn).ConfigureAwait(false);
                gmd.Player.PlaybackFinished += async e =>
                {
                    await Task.Delay(500);
                    Play();
                };
                Play();
            }

            if (trackCount > 1)
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Added {trackCount:#,##0} tracks to playback queue.").ConfigureAwait(false);
            else
            {
                var track = tracks.First();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Added {Formatter.Bold(Formatter.Sanitize(track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Author))} to the playback queue.").ConfigureAwait(false);
            }

            void Play()
            {
                var itemN = gmd.Dequeue();
                if (itemN == null)
                {
                    gmd.Player.Disconnect();
                    gmd.Player = null;
                    gmd.NowPlaying = default;
                    return;
                }

                var item = itemN.Value;
                gmd.NowPlaying = item;
                if (item.StartTime != null && item.Duration != null)
                    gmd.Player.PlayPartial(item.Track, item.StartTime ?? TimeSpan.Zero, item.Duration ?? item.Track.Length);
                else
                    gmd.Player.Play(item.Track);
            }
        }

        [Command("stop"), Description("Stops music playback.")]
        public async Task StopAsync(CommandContext ctx)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];
            var rmd = gmd.Empty();
            if (gmd.Player != null)
                gmd.Player.Stop();

            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Removed {rmd:#,##0} tracks from the queue.").ConfigureAwait(false);
        }

        [Command("pause"), Description("Pauses music playback.")]
        public async Task PauseAsync(CommandContext ctx)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];
            if (gmd.Player != null)
                gmd.Player.Pause();

            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Playback paused. Use the resume command to resume playback.").ConfigureAwait(false);
        }

        [Command("resume"), Description("Resumes music playback.")]
        public async Task ResumeAsync(CommandContext ctx)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];
            if (gmd.Player != null)
                gmd.Player.Resume();

            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Playback resumed.").ConfigureAwait(false);
        }

        [Command("skip"), Description("Skips current track. If playback is looped, the track will not be removed from the queue.")]
        public async Task SkipAsync(CommandContext ctx)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];
            var track = gmd.NowPlaying;
            if (gmd.Player != null)
                gmd.Player.Stop();

            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} {Formatter.Bold(Formatter.Sanitize(track.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Track.Author))} skipped.").ConfigureAwait(false);
        }

        [Command("volume"), Description("Changes playback volume.")]
        public async Task VolumeAsync(CommandContext ctx, [Description("Volume to set, in %. Has to be 0-150. Default is 100.")] int volume = 100)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            if (volume < 0 || volume > 150)
            {
                await ctx.RespondAsync("Invalid volume specified!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];
            if (gmd.Player != null)
                gmd.Player.SetVolume(volume);

            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Volume set to {volume}%.").ConfigureAwait(false);
        }

        [Command("restart"), Description("Restarts currently-played track.")]
        public async Task RestartAsync(CommandContext ctx)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];
            var track = gmd.NowPlaying;
            gmd.Restart();
            if (gmd.Player != null)
                gmd.Player.Stop();

            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} {Formatter.Bold(Formatter.Sanitize(track.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Track.Author))} restarted.").ConfigureAwait(false);
        }

        [Command("repeat"), Description("Changes repeat mode for the queue."), Aliases("loop")]
        public async Task RepeatAsync(CommandContext ctx, [Description("Repeat mode. Can be none, one, or all. Defaults to all.")] string mode = "all")
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            mode = mode.ToLowerInvariant();
            if (mode != "all" && mode != "one" && mode != "none")
            {
                await ctx.RespondAsync("Invalid repeat mode!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];

            var pMode = gmd.LoopMode;
            switch (mode)
            {
                default:
                case "all":
                    gmd.LoopMode = LoopMode.All;
                    break;

                case "one":
                    gmd.LoopMode = LoopMode.One;
                    if (pMode != LoopMode.One)
                        gmd.Queue.Insert(0, gmd.NowPlaying);
                    break;

                case "none":
                    gmd.LoopMode = LoopMode.None;
                    break;
            }
            if (pMode == LoopMode.One && gmd.LoopMode != LoopMode.One)
                gmd.Queue.RemoveAt(0);

            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Repeat mode set to {Formatter.Bold(gmd.LoopMode.ToString())}.").ConfigureAwait(false);
        }

        [Command("shuffle"), Description("Shuffles the queue.")]
        public async Task ShuffleAsync(CommandContext ctx)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];

            if (!gmd.IsShuffled)
            {
                gmd.Shuffle();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Queue is now shuffled.").ConfigureAwait(false);
            }
            else
            {
                gmd.IsShuffled = false;
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Queue is no longer shuffled.").ConfigureAwait(false);
            }
        }

        [Command("reshuffle"), Description("Shuffles the queue.")]
        public async Task ReshuffleAsync(CommandContext ctx)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];

            if (!gmd.IsShuffled)
            {
                gmd.Shuffle();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Queue is now shuffled.").ConfigureAwait(false);
            }
            else
            {
                gmd.Reshuffle();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Queue reshuffled.").ConfigureAwait(false);
            }
        }

        [Command("queue"), Description("Shows current playback queue.")]
        public async Task QueueAsync(CommandContext ctx)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];
            var interactivity = ctx.Client.GetInteractivity();

            var pageCount = gmd.Queue.Count / 10 + 1;
            if (gmd.Queue.Count % 10 == 0) pageCount--;
            var pages = gmd.Queue.Select(x => this.ToTrackString(x))
                .Select((s, i) => new { str = s, index = i })
                .GroupBy(x => x.index / 10)
                .Select(xg => new Page { Content = $"Now playing: {this.ToTrackString(gmd.NowPlaying)}\n\n{string.Join("\n", xg.Select(xa => $"`{xa.index + 1:00}` {xa.str}"))}\n\nPage {xg.Key + 1}/{pageCount}" });

            if (!pages.Any())
                pages = new List<Page>() { new Page { Content = "Queue is empty!" } };

            await interactivity.SendPaginatedMessage(ctx.Channel, ctx.User, pages, TimeSpan.FromMinutes(2), TimeoutBehaviour.Ignore);
        }

        [Command("remove"), Description("Removes a track from playback queue.")]
        public async Task RemoveAsync(CommandContext ctx, [Description("Index of the track to remove.")] int index)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];

            index--;
            if (index < 0 || index >= gmd.Queue.Count)
            {
                await ctx.RespondAsync("Invalid item!").ConfigureAwait(false);
                return;
            }

            var item = gmd.Queue[index];
            gmd.Queue.RemoveAt(index);
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Removed {Formatter.Bold(Formatter.Sanitize(item.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(item.Track.Author))} from the playback queue.").ConfigureAwait(false);
        }

        [Command("seek"), Description("Seeks to specified time in current track.")]
        public async Task SeekAsync(CommandContext ctx, [Description("Where in the current song to seek to.")] TimeSpan position)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];

            if (gmd.Player != null)
                gmd.Player.Seek(position);
        }

        [Command("forward"), Description("Forwards the track by specified amount of time.")]
        public async Task ForwardAsync(CommandContext ctx, [Description("By how much to forward the playback.")] TimeSpan offset)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];

            if (gmd.Player != null)
                gmd.Player.Seek(gmd.Player.CurrentState.PlaybackPosition + offset);
        }

        [Command("rewind"), Description("Rewinds the track by specified amount of time.")]
        public async Task RewindAsync(CommandContext ctx, [Description("By how much to rewind the playback.")] TimeSpan offset)
        {
            var vs = ctx.Member.VoiceState;
            var chn = vs.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You need to be in a voice channel!").ConfigureAwait(false);
                return;
            }

            var mbr = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (mbr != null && chn != mbr)
            {
                await ctx.RespondAsync("You need to be in the same voice channel!").ConfigureAwait(false);
                return;
            }

            var gmd = this.MusicQueues[ctx.Guild.Id];

            if (gmd.Player != null)
                gmd.Player.Seek(gmd.Player.CurrentState.PlaybackPosition - offset);
        }

        [Command("disconnectlavalink"), Description("Disconnects Lavalink client. Use before shutting the bot down."), Aliases("dclvl", "dclava"), RequireOwner]
        public async Task DisconnectLavalinkAsync(CommandContext ctx)
        {
            await this.Lavalink.StopAsync().ConfigureAwait(false);
            this.Lavalink = null;
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} Lavalink is now disconnected.").ConfigureAwait(false);
        }

        public override async Task BeforeExecutionAsync(CommandContext ctx)
        {
            if (this.Lavalink == null)
                this.Lavalink = ctx.Client.GetLavalink().GetNodeConnection(this.Shared.LavalinkEndpoint);

            if (this.Lavalink == null)
                this.Lavalink = await ctx.Client.GetLavalink().ConnectAsync(this.Shared.LavalinkConfiguration.ToLavalinkConfig()).ConfigureAwait(false);

            var gmd = this.MusicQueues.AddOrUpdate(ctx.Guild.Id, new GuildMusicData(this.RNG), (key, oldValue) => oldValue);

            await base.BeforeExecutionAsync(ctx);
        }

        private string ToTimespanString(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"m\:ss");
        }

        private string ToTrackString(MusicItem x)
        {
            return $"{Formatter.Bold(Formatter.Sanitize(x.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(x.Track.Author))} [{this.ToTimespanString(x.Track.Length)}] (added by {x.RequestedBy.DisplayName})";
        }
    }
}
