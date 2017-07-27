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

using System.Linq;
using System.Text.RegularExpressions;
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
        private static Regex MentionableRegex { get; } = new Regex(@"[a-z0-9]{3,}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private DiscordClient Discord { get; }
        private ConnectionStringProvider ConnectionStringProvider { get; }

        public PooperService(DiscordClient discord, ConnectionStringProvider csp)
        {
            this.Discord = discord;
            this.Discord.GuildMemberUpdated += this.Discord_GuildMemberUpdated;
            this.Discord.GuildMemberAdded += this.Discord_GuildMemberAdded;

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

        private Task Discord_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
            => this.RunPooperAsync(e.Guild, e.Member);

        private Task Discord_GuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs e)
            => this.RunPooperAsync(e.Guild, e.Member);

        private async Task RunPooperAsync(DiscordGuild gld, DiscordMember mbr)
        {
            var gid = (long)gld.Id;
            if (!await this.CheckWhitelistAsync(gid))
                return;

            var cmbr = gld.CurrentMember;
            if (cmbr == null)
            {
                this.Discord.Logger.LogError(LogEvent, "Current member in '{0}' ({1}) is null - could not process pooping!", gld.Name, gld.Id);
                return;
            }

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
            => mbr.DisplayName != "💩" && (mbr.DisplayName[0] < '0' || !MentionableRegex.IsMatch(mbr.DisplayName));
    }
}
