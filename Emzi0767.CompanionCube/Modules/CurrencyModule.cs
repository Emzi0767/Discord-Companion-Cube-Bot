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
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Emzi0767.CompanionCube.Exceptions;
using Emzi0767.CompanionCube.Services;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("currency", CanInvokeWithoutSubcommand = true), Aliases("money", "shekels"), Description("Currency-related commands. Invoking without subcommands will show current balance."), NotBlocked]
    public sealed class CurrencyModule
    {
        private DatabaseClient Database { get; }
        private SharedData Shared { get; }

        public CurrencyModule(DatabaseClient database, SharedData shared)
        {
            this.Database = database;
            this.Shared = shared;
        }

        [Command("send"), Aliases("transfer", "wire"), Description("Sends specified amount of currency to another user.")]
        public async Task SendAsync(CommandContext ctx, [Description("Member to send currency to.")] DiscordMember target, [Description("Amount of currency to send.")] long amount)
        {
            DiscordEmbed embed;

            if (target.IsBot)
            {
                embed = new DiscordEmbed
                {
                    Title = string.Concat("Currency transfer error."),
                    Description = string.Concat("Bots cannot own currency."),
                    Color = 0x628958
                };
                await ctx.RespondAsync("", embed: embed).ConfigureAwait(false);
                return;
            }

            if (target.Id == ctx.User.Id)
            {
                embed = new DiscordEmbed
                {
                    Title = string.Concat("Currency transfer error."),
                    Description = string.Concat("Cannot send currency to yourself."),
                    Color = 0x628958
                };
                await ctx.RespondAsync("", embed: embed).ConfigureAwait(false);
                return;
            }

            if (amount <= 0)
            {
                embed = new DiscordEmbed
                {
                    Title = string.Concat("Currency transfer error."),
                    Description = string.Concat("You need to send at least ", this.Shared.CurrencySymbol, " 1."),
                    Color = 0x628958
                };
                await ctx.RespondAsync("", embed: embed).ConfigureAwait(false);
                return;
            }

            try
            {
                await this.Database.TransferCurrencyAsync(ctx.User.Id, target.Id, amount).ConfigureAwait(false);
            }
            catch (CurrencyException ex)
            {
                embed = new DiscordEmbed
                {
                    Title = string.Concat("Currency transfer error."),
                    Description = string.Concat("An error occured when transferring currency: ", ex.Message),
                    Color = 0x628958
                };
                await ctx.RespondAsync("", embed: embed).ConfigureAwait(false);
                return;
            }

            embed = new DiscordEmbed
            {
                Title = string.Concat("Currency transfer successful."),
                Description = string.Concat("You sent ", this.Shared.CurrencySymbol, " ", amount.ToString("#,##0"), " to ", target.Mention, "!"),
                Color = 0x628958
            };
            await ctx.RespondAsync("", embed: embed).ConfigureAwait(false);
        }

        [Command("grant"), Description("Grants specified user a specified amount of money."), Hidden, RequireOwner]
        public async Task GrantAsync(CommandContext ctx, [Description("Member to grant currency to.")] DiscordMember target, [Description("Amount of currency to grant. This can be negative.")] long amount)
        {
            await this.Database.IssueCurrencyAsync(target.Id, amount).ConfigureAwait(false);

            var embed = new DiscordEmbed
            {
                Title = string.Concat("Currency grant successful."),
                Description = string.Concat(this.Shared.CurrencySymbol, " ", amount.ToString("#,##0"), amount >= 0 ? " granted to " : " removed from ", target.Mention, "!"),
                Color = 0x628958
            };
            await ctx.RespondAsync("", embed: embed).ConfigureAwait(false);
        }

        public async Task ExecuteGroup(CommandContext ctx, [Description("Member to check balance for. If not specified, defaults to invoker.")] DiscordMember mbr = null)
        {
            var usr = mbr ?? ctx.Member;
            DiscordEmbed embed = null;

            if (usr.IsBot)
            {
                embed = new DiscordEmbed
                {
                    Title = string.Concat("Currency check error."),
                    Description = string.Concat("Bots cannot own currency."),
                    Color = 0x628958
                };
                await ctx.RespondAsync("", embed: embed).ConfigureAwait(false);
                return;
            }

            var shekels = await this.Database.GetCurrencyAsync(usr.Id).ConfigureAwait(false);

            embed = new DiscordEmbed
            {
                Title = string.Concat("Account balance for ", usr.DisplayName),
                Description = string.Concat(this.Shared.CurrencySymbol, " ", shekels.ToString("#,##0")),
                Color = 0x628958
            };
            await ctx.RespondAsync("", embed: embed).ConfigureAwait(false);
        }
    }
}