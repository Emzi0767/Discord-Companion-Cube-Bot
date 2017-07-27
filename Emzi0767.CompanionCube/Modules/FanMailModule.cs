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

using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Emzi0767.CompanionCube.Attributes;
using Emzi0767.CompanionCube.Services;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("mail")]
    [Description("Message Emzi while he's in self-imposed exile.")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [Hidden]
    [RequireDirectMessage, NotBlacklisted]
    public sealed class FanMailModule : BaseCommandModule
    {
        private DatabaseContext Database { get; }
        private MailmanService Mailman { get; }

        public FanMailModule(DatabaseContext db, MailmanService mailman)
        {
            this.Database = db;
            this.Mailman = mailman;
        }

        [Command("set")]
        [Description("Sets the mailbox guild and channel to use, and enables the mailbox.")]
        [RequireOwner]
        public async Task SetAsync(CommandContext ctx,
            [Description("Guild to use as mailbox.")] DiscordGuild guild,
            [Description("Channel to use as mailbox.")] DiscordChannel channel)
        {
            if (channel.Guild != guild)
            {
                await ctx.RespondAsync("Must be a channel within the specified guild.");
                return;
            }

            await this.Mailman.EnableAsync(this.Database, guild.Id, channel.Id);
            await ctx.RespondAsync($"Mailman enabled in {guild.Id}::{channel.Id}.");
        }

        [Command("unset")]
        [Description("Disables the mailbox.")]
        [RequireOwner]
        public async Task UnsetAsync(CommandContext ctx)
        {
            await this.Mailman.DisableAsync(this.Database);
            await ctx.RespondAsync("Mailman disabled.");
        }

        [Command("status")]
        [Description("Checks mailbox status.")]
        [RequireOwner]
        public async Task StatusAsync(CommandContext ctx)
        {
            var conf = this.Mailman.GetStatus();
            await ctx.RespondAsync(conf == null
                ? "Disabled"
                : $"G: {conf.Guild} / C: {conf.Channel}");
        }

        [Command("send")]
        [Description("Sends a message to the mailbox. Can be used thrice per 15 minutes.")]
        [Cooldown(3, 900, CooldownBucketType.User)]
        public async Task SendAsync(CommandContext ctx,
            [Description("Contents of the message to send."), RemainingText] string contents)
        {
            if (contents != null && contents.Length > 1000)
            {
                await ctx.RespondAsync("Must be less than 1000 characters.");
                return;
            }

            var result = await this.Mailman.SendMessageAsync(
                this.Database,
                ctx.User,
                ctx.Channel,
                contents,
                ctx.Message.Attachments?.Select(x => (x.FileName, new Uri(x.Url), x.FileSize)) ?? Enumerable.Empty<(string, Uri, int)>());

            if (!result)
                await ctx.RespondAsync("Message wasn't sent: mailman is not enabled.");
            else
                await ctx.RespondAsync("Message sent. Responses will be delivered via the same DM.");
        }
    }
}
