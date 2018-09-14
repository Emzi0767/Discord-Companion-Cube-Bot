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
