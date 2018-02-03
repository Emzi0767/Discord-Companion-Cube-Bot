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
using System.Net.Http;

namespace Emzi0767.CompanionCube.Services
{
    public sealed class SharedData
    {
        public ConcurrentDictionary<ulong, string> ChannelPrefixes { get; }
        public ConcurrentDictionary<ulong, string> GuildPrefixes { get; }
        public ConcurrentDictionary<ulong, double> ShekelRates { get; }
        public ConcurrentHashSet<ulong> BlockedUsers { get; }
        public ConcurrentHashSet<ulong> BlockedChannels { get; }
        public ConcurrentHashSet<ulong> BlockedGuilds { get; }
        public string CurrencySymbol { get; }
        public DateTime ProcessStarted { get; }
        public string Game { get; }
        public HttpClient Http { get; }

        public SharedData(ConcurrentDictionary<ulong, string> cprefixes, ConcurrentDictionary<ulong, string> gprefixes, ConcurrentHashSet<ulong> blockedUsers, ConcurrentHashSet<ulong> blockedChannels, 
            ConcurrentHashSet<ulong> blockedGuilds, string currencySymbol, DateTime processStarted, string game, ConcurrentDictionary<ulong, double> shekelRates)
        {
            this.ChannelPrefixes = cprefixes;
            this.GuildPrefixes = gprefixes;
            this.BlockedUsers = blockedUsers;
            this.BlockedChannels = blockedChannels;
            this.BlockedGuilds = blockedGuilds;
            this.CurrencySymbol = currencySymbol;
            this.ProcessStarted = processStarted;
            this.Game = game;
            this.Http = new HttpClient();
            this.ShekelRates = shekelRates;
        }

        public string TimeSpanToString(TimeSpan ts)
        {
            var indices = new List<string>();
            var prev = false;

            if (ts.Days > 0)
            {
                indices.Add(string.Format("{0:#,##0} day{1}", ts.Days, ts.Days == 1 ? "" : "s"));
                prev = true;
            }
            
            if (ts.Hours > 0 || prev)
            {
                indices.Add(string.Format("{0} hour{1}", ts.Hours, ts.Hours == 1 ? "" : "s"));
                prev = true;
            }
            
            if (ts.Minutes > 0 || prev)
            {
                indices.Add(string.Format("{0} minute{1}", ts.Minutes, ts.Minutes == 1 ? "" : "s"));
                prev = true;
            }
            
            if (ts.Seconds > 0 || prev)
            {
                indices.Add(string.Format("{0} second{1}", ts.Seconds, ts.Seconds == 1 ? "" : "s"));
                prev = true;
            }

            if (indices.Any())
                return string.Join(", ", indices);
            else
                return "0 seconds";
        }
    }
}