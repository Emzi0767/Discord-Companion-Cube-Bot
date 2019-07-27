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
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Emzi0767.CompanionCube.Attributes;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("lab")]
    [Description("Commands for The Lab.")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [RequireAutism]
    public sealed class LabModule : BaseCommandModule
    {
        [Command("rolecolour")]
        [Aliases("rcolor", "rclr", "rc")]
        [Description("Facilitates <@!255950165200994307>' urge to change role colours every day, without human interaction. This will not create new roles.")]
        [Cooldown(1, 60, CooldownBucketType.User)]
        public async Task RoleColourAsync(CommandContext ctx, [RemainingText, Description("New colour you want for your role.")] DiscordColor newColor)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);

            var mbr = ctx.Member;
            var rol = mbr.Roles.FirstOrDefault(x => !x.IsManaged && x.Color.Value != 0);
            if (rol == null)
            {
                await ctx.RespondAsync("You don't have a coloured role I can alter. Please contact a nearby administrator.").ConfigureAwait(false);
                return;
            }

            var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"Modifying {rol.Mention}...").Build()).ConfigureAwait(false);
            var embed = new DiscordEmbedBuilder(msg.Embeds.First());
            try
            {
                await rol.ModifyAsync(x => x.Color = newColor).ConfigureAwait(false);
                await msg.ModifyAsync(embed: AppendDescription(embed, DiscordEmoji.FromName(ctx.Client, ":msokhand:"))).ConfigureAwait(false);
            }
            catch
            {
                await msg.ModifyAsync(embed: AppendDescription(embed, "SIKE!")).ConfigureAwait(false);
            }

            DiscordEmbed AppendDescription(DiscordEmbedBuilder e, string t)
                => e.WithDescription($"{e.Description} {t}").Build();
        }
    }
}
