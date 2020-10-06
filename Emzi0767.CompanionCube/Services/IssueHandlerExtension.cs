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
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Emzi0767.CompanionCube.Data;

namespace Emzi0767.CompanionCube.Services
{
    internal sealed class IssueHandlerExtension : BaseExtension
    {
        private const string IssueLinkBase = "https://github.com/DSharpPlus/DSharpPlus/issues/";

        private static Regex IssueRegex { get; } = new Regex(@"##(?!0)(?<iss>\d{1,5}(?!\d))", RegexOptions.ECMAScript | RegexOptions.Compiled);

        private HashSet<ulong> AllowedChannels { get; }
        private HashSet<ulong> AllowedGuilds { get; }

        private bool IsEnabled { get; }

        internal IssueHandlerExtension(CompanionCubeConfigGitHub cfg)
        {
            this.IsEnabled = cfg.IsEnabled;

            if (this.IsEnabled)
            {
                this.AllowedChannels = new HashSet<ulong>(cfg.Channels);
                this.AllowedGuilds = new HashSet<ulong>(cfg.Guilds);
            }
        }

        protected override void Setup(DiscordClient client)
        {
            if (this.IsEnabled)
                client.MessageCreated += this.Client_MessageCreated;
        }

        private async Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Message.Content == null)
                return;

            if (e.Guild == null)
                return;

            if (!this.AllowedChannels.Contains(e.Channel.Id) && !this.AllowedGuilds.Contains(e.Guild.Id))
                return;

            var cnt = e.Message.Content;
            var ms = IssueRegex.Matches(cnt);
            if (ms.Count <= 0 || !ms.Any(x => x.Success))
                return;

            var links = string.Join("\n", ms.Select(x => x.Groups["iss"].Value)
                .Select(x => string.Create(IssueLinkBase.Length + x.Length, x, FormatIssueLink)));

            await e.Message.RespondAsync(links);

            static void FormatIssueLink(Span<char> buff, string issue)
            {
                issue.AsSpan().CopyTo(buff.Slice(buff.Length - issue.Length));
                IssueLinkBase.AsSpan().CopyTo(buff);
            }
        }
    }
}
