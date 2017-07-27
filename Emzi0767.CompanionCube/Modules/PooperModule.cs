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

using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Emzi0767.CompanionCube.Attributes;
using Emzi0767.CompanionCube.Services;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("poop"), Aliases("pooper", "💩")]
    [Description("Controls the pooper and poops members.")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [OwnerOrPermission(Permissions.ManageNicknames)]
    [RequireGuild]
    public sealed class PooperModule : BaseCommandModule
    {
        private PooperService Pooper { get; }

        public PooperModule(PooperService pooper)
        {
            this.Pooper = pooper;
        }

        [GroupCommand]
        public async Task PoopAsync(CommandContext ctx, [RemainingText, Description("Member to poop.")] DiscordMember mbr)
        {
            await this.Pooper.PoopAsync(mbr);
            await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":mspoop:"));
        }

        [Command("control"), Aliases("ctl", "configure", "cfg")]
        [Description("Controls the pooper in the current guild.")]
        public async Task ControlAsync(CommandContext ctx, 
            [Description("Whether to enable the pooper.")] bool enable,
            [RemainingText, Description("Note to put on the whitelist entry.")] string comment = null)
        {
            await this.Pooper.ControlAsync(ctx.Guild, enable, comment);
            await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:"));
        }

        [Command("enable"), Aliases("on")]
        [Description("Enables the pooper in the current guild.")]
        public Task EnableAsync(CommandContext ctx, [RemainingText, Description("Note to put on the whitelist entry.")] string comment = null)
            => this.ControlAsync(ctx, true, comment);

        [Command("disable"), Aliases("off")]
        [Description("Disables the pooper in the current guild.")]
        public Task DisableAsync(CommandContext ctx)
            => this.ControlAsync(ctx, false);
    }
}
