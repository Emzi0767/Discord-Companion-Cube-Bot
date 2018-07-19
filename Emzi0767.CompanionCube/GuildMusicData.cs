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
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Emzi0767.CompanionCube.Services;

namespace Emzi0767.CompanionCube
{
    public sealed class GuildMusicData
    {
        public bool IsShuffled { get; set; } = false;
        public LoopMode LoopMode { get; set; } = LoopMode.None;

        public List<MusicItem> Queue { get; } = new List<MusicItem>();
        public MusicItem NowPlaying { get; set; }

        private CSPRNG RNG { get; }

        public LavalinkGuildConnection Player { get; set; }

        public GuildMusicData(CSPRNG rng)
        {
            this.RNG = rng;
        }

        public int Empty()
        {
            lock (this.Queue)
            {
                int itemCount = this.Queue.Count;
                this.Queue.Clear();
                return itemCount;
            }
        }

        public void Shuffle()
        {
            this.IsShuffled = true;
            lock (this.Queue)
            {
                this.Queue.Sort(new Randomizer(this.RNG));
            }
        }

        public void Reshuffle()
        {
            lock (this.Queue)
            {
                this.Queue.Sort(new Randomizer(this.RNG));
            }
        }

        public void Enqueue(MusicItem item)
        {
            lock (this.Queue)
            {
                if (!this.IsShuffled)
                {
                    this.Queue.Add(item);
                    return;
                }

                if (this.Queue.Any())
                {
                    var index = this.RNG.Next(0, this.Queue.Count);
                    this.Queue.Insert(index, item);
                }
                else
                {
                    this.Queue.Add(item);
                }
            }
        }

        public MusicItem? Dequeue()
        {
            lock (this.Queue)
            {
                if (this.Queue.Count == 0)
                    return null;

                if (this.LoopMode == LoopMode.None)
                {
                    var item = this.Queue[0];
                    this.Queue.RemoveAt(0);
                    return item;
                }

                if (this.LoopMode == LoopMode.One)
                {
                    var item = this.Queue[0];
                    return item;
                }

                if (this.LoopMode == LoopMode.All)
                {
                    var item = this.Queue[0];
                    this.Queue.RemoveAt(0);
                    this.Queue.Add(item);
                    return item;
                }
            }

            return null;
        }

        public void Remove(int index)
        {
            lock (this.Queue)
            {
                this.Queue.RemoveAt(index);
            }
        }

        public void Restart()
        {
            lock (this.Queue)
            {
                this.Queue.Insert(0, this.NowPlaying);
            }
        }
    }

    public enum LoopMode
    {
        None,
        One,
        All
    }

    public struct MusicItem
    {
        public LavalinkTrack Track { get; }
        public TimeSpan? StartTime { get; }
        public TimeSpan? Duration { get; }
        public DiscordMember RequestedBy { get; }

        public MusicItem(LavalinkTrack track, DiscordMember requester)
        {
            this.Track = track;
            this.StartTime = null;
            this.Duration = null;
            this.RequestedBy = requester;
        }

        public MusicItem(LavalinkTrack track, TimeSpan startTime, TimeSpan duration, DiscordMember requester)
        {
            this.Track = track;
            this.StartTime = startTime;
            this.Duration = duration;
            this.RequestedBy = requester;
        }
    }

    public class Randomizer : IComparer<MusicItem>
    {
        public CSPRNG RNG { get; }

        public Randomizer(CSPRNG rng)
        {
            this.RNG = rng;
        }

        public int Compare(MusicItem x, MusicItem y)
        {
            var val1 = this.RNG.Next();
            var val2 = this.RNG.Next();

            if (val1 > val2)
                return 1;
            if (val1 < val2)
                return -1;
            return 0;
        }
    }
}
