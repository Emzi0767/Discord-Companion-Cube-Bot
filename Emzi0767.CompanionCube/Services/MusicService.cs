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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Emzi0767.CompanionCube.Data;

namespace Emzi0767.CompanionCube.Services
{
    /// <summary>
    /// Provides a persistent way of tracking music in various guilds.
    /// </summary>
    public sealed class MusicService
    {
        private LavalinkService Lavalink { get; }
        private SecureRandom RNG { get; }
        private ConcurrentDictionary<ulong, GuildMusicData> MusicData { get; }
        private DiscordClient Discord { get; }

        /// <summary>
        /// Creates a new instance of this music service.
        /// </summary>
        /// <param name="redis">Redis client to use for persistence.</param>
        /// <param name="rng">Cryptographically-secure random number generator implementaion.</param>
        public MusicService(SecureRandom rng, LavalinkService lavalink, CompanionCubeBot bot)
        {
            this.Lavalink = lavalink;
            this.RNG = rng;
            this.MusicData = new ConcurrentDictionary<ulong, GuildMusicData>();
            this.Discord = bot.Discord;

            this.Lavalink.TrackExceptionThrown += this.Lavalink_TrackExceptionThrown;
        }

        /// <summary>
        /// Gets or creates a dataset for specified guild.
        /// </summary>
        /// <param name="guild">Guild to get or create dataset for.</param>
        /// <returns>Resulting dataset.</returns>
        public Task<GuildMusicData> GetOrCreateDataAsync(DiscordGuild guild)
        {
            if (this.MusicData.TryGetValue(guild.Id, out var gmd))
                return Task.FromResult(gmd);

            gmd = this.MusicData.AddOrUpdate(guild.Id, new GuildMusicData(guild, this.RNG, this.Lavalink), (k, v) => v);
            return Task.FromResult(gmd);
        }

        /// <summary>
        /// Loads tracks from specified URL.
        /// </summary>
        /// <param name="uri">URL to load tracks from.</param>
        /// <returns>Loaded tracks.</returns>
        public Task<LavalinkLoadResult> GetTracksAsync(Uri uri)
            => this.Lavalink.LavalinkNode.Rest.GetTracksAsync(uri);

        /// <summary>
        /// Shuffles the supplied track list.
        /// </summary>
        /// <param name="tracks">Collection of tracks to shuffle.</param>
        /// <returns>Shuffled track collection.</returns>
        public IEnumerable<LavalinkTrack> Shuffle(IEnumerable<LavalinkTrack> tracks)
            => tracks.OrderBy(x => this.RNG.Next());

        private async Task Lavalink_TrackExceptionThrown(LavalinkGuildConnection con, TrackExceptionEventArgs e)
        {
            if (e.Player?.Guild == null)
                return;

            if (!this.MusicData.TryGetValue(e.Player.Guild.Id, out var gmd))
                return;

            await gmd.CommandChannel.SendMessageAsync($"{DiscordEmoji.FromName(this.Discord, ":msfrown:")} A problem occured while playing {Formatter.Bold(Formatter.Sanitize(e.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(e.Track.Author))}:\n{e.Error}");
        }
    }
}
