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

using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Newtonsoft.Json;

namespace Emzi0767.CompanionCube.Data
{
    /// <summary>
    /// Represents a single music queue item.
    /// </summary>
    public struct MusicItem
    {
        /// <summary>
        /// Gets the track to be played.
        /// </summary>
        [JsonIgnore]
        public LavalinkTrack Track { get; }

        /// <summary>
        /// Gets the member who requested the track.
        /// </summary>
        [JsonIgnore]
        public DiscordMember RequestedBy { get; }

        /// <summary>
        /// Constructs a new music queue items.
        /// </summary>
        /// <param name="track">Track to play.</param>
        /// <param name="requester">Who requested the track.</param>
        public MusicItem(LavalinkTrack track, DiscordMember requester)
        {
            this.Track = track;
            this.RequestedBy = requester;
        }
    }

    public struct MusicItemSerializable
    {
        [JsonProperty("track")]
        public string Track { get; set; }

        [JsonProperty("member_id")]
        public ulong MemberId { get; set; }

        public MusicItemSerializable(MusicItem mi)
        {
            this.Track = mi.Track.TrackString;
            this.MemberId = mi.RequestedBy.Id;
        }
    }
}
