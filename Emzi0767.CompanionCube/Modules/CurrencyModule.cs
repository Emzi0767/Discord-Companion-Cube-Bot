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

using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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
            DiscordEmbedBuilder embed = null;

            if (target.IsBot)
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = string.Concat("Currency transfer error."),
                    Description = string.Concat("Bots cannot own currency."),
                    Color = new DiscordColor(0x628958)
                };
                await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
                return;
            }

            if (target.Id == ctx.User.Id)
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = string.Concat("Currency transfer error."),
                    Description = string.Concat("Cannot send currency to yourself."),
                    Color = new DiscordColor(0x628958)
                };
                await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
                return;
            }

            if (amount <= 0)
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = string.Concat("Currency transfer error."),
                    Description = string.Concat("You need to send at least ", this.Shared.CurrencySymbol, " 1."),
                    Color = new DiscordColor(0x628958)
                };
                await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
                return;
            }

            try
            {
                await this.Database.TransferCurrencyAsync(ctx.User.Id, target.Id, amount).ConfigureAwait(false);
            }
            catch (CurrencyException ex)
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = string.Concat("Currency transfer error."),
                    Description = string.Concat("An error occured when transferring currency: ", ex.Message),
                    Color = new DiscordColor(0x628958)
                };
                await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
                return;
            }

            embed = new DiscordEmbedBuilder
            {
                Title = string.Concat("Currency transfer successful."),
                Description = string.Concat("You sent ", this.Shared.CurrencySymbol, " ", amount.ToString("#,##0"), " to ", target.Mention, "!"),
                Color = new DiscordColor(0x628958)
            };
            await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("grant"), Description("Grants specified user a specified amount of money."), Hidden, RequireOwner]
        public async Task GrantAsync(CommandContext ctx, [Description("Member to grant currency to.")] DiscordMember target, [Description("Amount of currency to grant. This can be negative.")] long amount)
        {
            await this.Database.IssueCurrencyAsync(target.Id, amount).ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder
            {
                Title = string.Concat("Currency grant successful."),
                Description = string.Concat(this.Shared.CurrencySymbol, " ", amount.ToString("#,##0"), amount >= 0 ? " granted to " : " removed from ", target.Mention, "!"),
                Color = new DiscordColor(0x628958)
            };
            await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        public async Task ExecuteGroupAsync(CommandContext ctx, [Description("Member to check balance for. If not specified, defaults to invoker.")] DiscordMember mbr = null)
        {
            var usr = mbr ?? ctx.Member;
            DiscordEmbedBuilder embed = null;

            if (usr.IsBot)
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = string.Concat("Currency check error."),
                    Description = string.Concat("Bots cannot own currency."),
                    Color = new DiscordColor(0x628958)
                };
                await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
                return;
            }

            var shekels = await this.Database.GetCurrencyAsync(usr.Id).ConfigureAwait(false);

            embed = new DiscordEmbedBuilder
            {
                Title = string.Concat("Account balance for ", usr.DisplayName),
                Description = string.Concat(this.Shared.CurrencySymbol, " ", shekels.ToString("#,##0")),
                Color = new DiscordColor(0x628958)
            };
            await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        /* Disabled, was here by accident.
         * 
         * [Group("gamble"), Aliases("gambling"), Description("Lets you gamble your life's savings.")]
         * public class CurrencyGamblingModule
         * {
         *     private DatabaseClient Database { get; }
         *     private SharedData Shared { get; }
         *     private IDictionary<DiscordEmoji, RpsResponse> RpsResponses { get; }
         *     private IDictionary<RpsResponseType, DiscordEmoji> RpsResponseEmotes { get; }
         * 
         *     public CurrencyGamblingModule(DiscordClient client, DatabaseClient database, SharedData shared)
         *     {
         *         this.Database = database;
         *         this.Shared = shared;
         * 
         *         var emotes_s = new[] 
         *         { 
         *             new[] { ":fist:", ":left_facing_fist:", ":right_facing_fist:", ":punch:" }, 
         *             new[] { ":hand_splayed:", ":raised_hand:", ":raised_back_of_hand:" }, 
         *             new[] { ":v:" } 
         *         };
         *         var emotes_t = new[] { "", ":skin-tone-1:", ":skin-tone-2:", ":skin-tone-3:", ":skin-tone-4:", ":skin-tone-5:" };
         *         this.RpsResponses = new Dictionary<DiscordEmoji, RpsResponse>();
         *         this.RpsResponseEmotes = new Dictionary<RpsResponseType, DiscordEmoji>();
         * 
         *         for (var i = 0; i < emotes_s.Length; i++)
         *         {
         *             this.RpsResponseEmotes[(RpsResponseType)(i + 1)] = DiscordEmoji.FromName(client, emotes_s[i][0]);
         * 
         *             for (var j = 0; j < emotes_s[i].Length; j++)
         *             {
         *                 var rpsrs = emotes_t.Select(xs => DiscordEmoji.FromName(client, string.Concat(emotes_s[i][j], xs)))
         *                     .Select(xe => new RpsResponse { Emote = xe, Type = (RpsResponseType)(i + 1) });
         *                 foreach (var rpsr in rpsrs)
         *                     this.RpsResponses[rpsr.Emote] = rpsr;
         *             }
         *         }
         *     }
         * 
         *     [Command("rps"), Description("Play a game of rock-paper-scissors against the bot.")]
         *     public async Task RockPaperScissorsAsync(CommandContext ctx, [Description("Amount of currency to bet.")] int amount)
         *     {
         *         var interactivity = ctx.Client.GetInteractivityModule();
         * 
         *         var rpsr = 0;
         *         var buff = new byte[8];
         *         using (var rng = RandomNumberGenerator.Create())
         *             rng.GetBytes(buff);
         *         
         *         
         *         var msg = await ctx.RespondAsync("React with one of :fist:, :left_facing_fist:, :right_facing_fist:, :punch:, :hand_splayed:, :raised_hand:, :raised_back_of_hand:, or :v: to this message within 30s.").ConfigureAwait(false);
         * 
         *         var em = await interactivity.WaitForMessageReactionAsync(xe => this.RpsResponses.Keys.Contains(xe), msg, TimeSpan.FromSeconds(30), ctx.User.Id).ConfigureAwait(false);
         *         if (em == null)
         *         {
         *             await ctx.RespondAsync("Too slow, your bet is lost.").ConfigureAwait(false);
         *             return;
         *         }
         * 
         *         var usrr = this.RpsResponses[em.Emoji];
         *         var usrt = usrr.Type;
         * 
         *         var t1 = rpsr;
         *         var t2 = (int)usrt;
         *         if ((t1 == 3 && t2 == 1) || t2 > t1)
         *         {
         *             await ctx.RespondAsync(":trophy: A winner is you!").ConfigureAwait(false);
         *         }
         *         else if ((t1 == 1 && t2 == 3) || t2 < t1)
         *         {
         *             await ctx.RespondAsync(":confused: Nope, try again!").ConfigureAwait(false);
         *         }
         *         else if (t2 == t1)
         *         {
         *             await ctx.RespondAsync(":shrug: Draw, try again!").ConfigureAwait(false);
         *         }
         *         else
         *         {
         *             await ctx.RespondAsync("<:onswat:332690977926152204> I'm hosed, let someone know.").ConfigureAwait(false);
         *         }
         *     }
         * }
         */
    }

    public enum RpsResponseType
    {
        Unknown = 0,
        Rock = 1,
        Paper = 2,
        Scissors = 3
    }

    public struct RpsResponse
    {
        public DiscordEmoji Emote { get; set; }
        public RpsResponseType Type { get; set; }
    }
}