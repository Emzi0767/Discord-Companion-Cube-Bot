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

using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Emzi0767.CompanionCube.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emzi0767.CompanionCube.Services
{
    public sealed class PooperService
    {
        private static EventId LogEvent { get; } = new EventId(1002, "CCPoop");

        private DiscordClient Discord { get; }
        private ConnectionStringProvider ConnectionStringProvider { get; }

        public PooperService(DiscordClient discord, ConnectionStringProvider csp)
        {
            this.Discord = discord;
            this.Discord.GuildMemberUpdated += this.Discord_GuildMemberUpdated;

            this.ConnectionStringProvider = csp;
        }

        public async Task PoopAsync(DiscordMember mbr)
        {
            var gld = mbr.Guild;
            var cmbr = gld.CurrentMember;
            if (cmbr == null)
            {
                this.Discord.Logger.LogError(LogEvent, "Current member in '{0}' ({1}) is null - could not process pooping!", gld.Name, gld.Id);
                return;
            }

            if (!this.CanPoop(mbr, cmbr, force: true))
                return;

            await DoPoopAsync(mbr);
        }

        public async Task ControlAsync(DiscordGuild gld, bool enable, string comment = null)
        {
            var gid = (long)gld.Id;
            using var db = new DatabaseContext(this.ConnectionStringProvider);

            if (enable)
            {
                if (await db.PooperWhitelist.AnyAsync(x => x.GuildId == gid))
                    return;

                await db.PooperWhitelist.AddAsync(new DatabasePooperWhitelist { GuildId = gid, Comment = comment });
            }
            else
            {
                var pooperwl = await db.PooperWhitelist.FirstOrDefaultAsync(x => x.GuildId == gid);
                if (pooperwl == null)
                    return;

                db.PooperWhitelist.Remove(pooperwl);
            }

            await db.SaveChangesAsync();
        }

        private async Task Discord_GuildMemberUpdated(GuildMemberUpdateEventArgs e)
        {
            var gid = (long)e.Guild.Id;
            if (!await this.CheckWhitelistAsync(gid))
                return;

            var gld = e.Guild;
            var cmbr = gld.CurrentMember;
            if (cmbr == null)
            {
                this.Discord.Logger.LogError(LogEvent, "Current member in '{0}' ({1}) is null - could not process pooping!", gld.Name, gld.Id);
                return;
            }

            var mbr = e.Member;
            if (!this.CanPoop(mbr, cmbr))
                return;

            await DoPoopAsync(mbr);
        }

        private async Task<bool> CheckWhitelistAsync(long guildId)
        {
            using (var db = new DatabaseContext(this.ConnectionStringProvider))
                return await db.PooperWhitelist.AnyAsync(x => x.GuildId == guildId);
        }

        private bool CanPoop(DiscordMember mbr, DiscordMember cmbr, bool force = false)
        {
            if (mbr == null || mbr.Roles == null)
            {
                this.Discord.Logger.LogError(LogEvent, "Member '{0}' ({1}) state invalid - could not process pooping!", mbr.DisplayName, mbr.Id);
                return false;
            }

            if (!IsPoopable(mbr) && !force)
                return false;

            if (!CanRename(cmbr))
                return false;

            if (mbr.Hierarchy >= cmbr.Hierarchy)
                return false;

            return true;
        }

        private static Task DoPoopAsync(DiscordMember mbr)
            => mbr.ModifyAsync(x => x.Nickname = "💩");

        private static bool CanRename(DiscordMember mbr)
        {
            var perms = mbr.Roles.Aggregate(Permissions.None, (p, r) => p |= r.Permissions);
            return (perms & Permissions.ManageNicknames) == Permissions.ManageNicknames;
        }

        public static bool IsPoopable(DiscordMember mbr)
            => mbr.DisplayName[0] < '0';
    }
}
