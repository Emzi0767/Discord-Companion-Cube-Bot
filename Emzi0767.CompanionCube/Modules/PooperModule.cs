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
