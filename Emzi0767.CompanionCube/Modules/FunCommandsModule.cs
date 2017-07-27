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
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Emzi0767.CompanionCube.Attributes;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("fun")]
    [Description("Commands for fun and great justice.")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [NotBlacklisted]
    [RequireGuild]
    public class FunCommandsModule : BaseCommandModule
    {
        private SecureRandom RNG { get; }
        private Regex DiceRegex { get; } = new Regex(@"^(?<count>\d+)?d(?<sides>\d+)$", RegexOptions.Compiled | RegexOptions.ECMAScript);

        public FunCommandsModule(SecureRandom rng)
        {
            this.RNG = rng;
        }

        [Command("avatar"), Aliases("pfp"), Description("<@!255950165200994307>")]
        public async Task AvatarAsync(CommandContext ctx, [RemainingText, Description("User whose avatar to display.")] DiscordUser user = null)
        {
            user ??= ctx.User;
            var avatar = user.AvatarHash != null
                ? user.GetAvatarUrl(user.AvatarHash[0..2].SequenceEqual("a_") ? ImageFormat.Gif : ImageFormat.Png, 1024)
                : user.DefaultAvatarUrl;

            await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                .WithAuthor(name: $"{user.Username}#{user.Discriminator}", url: null, iconUrl: avatar)
                .WithImageUrl(avatar)
                .Build());
        }

        [Command("choice"), Aliases("pick"), Description("Chooses a random option from supplied ones.")]
        public async Task ChoiceAsync(CommandContext ctx, [Description("Options to choose from.")] params string[] choices)
        {
            if (choices?.Any() != true)
                throw new ArgumentException("You need to specify at least 1 item to choose from.", nameof(choices));

            await ctx.TriggerTypingAsync();
            var x = choices[this.RNG.Next(choices.Length)].Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere");
            await ctx.RespondAsync($"\u200b{x}");
        }

        [Command("choicex"), Aliases("pickx"), Description("Chooses a random option from supplied ones, specified number of times.")]
        public async Task ChoiceAsync(CommandContext ctx, [Description("Number of times to perform the choice.")] int count, [Description("Options to choose from.")] params string[] choices)
        {
            if (count < 2 && count > 10)
                throw new ArgumentOutOfRangeException(nameof(count), "You need to specify a number between 2 and 10 inclusive.");

            if (choices?.Any() != true)
                throw new ArgumentException("You need to specify at least 1 item to choose from.", nameof(choices));

            await ctx.TriggerTypingAsync();

            var choice = new string[count];
            var sb = new StringBuilder();
            for (var i = 0; i < count; i++)
            {
                choice[i] = choices[this.RNG.Next(choices.Length)].Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere");
                sb.Append("Choice ").Append(i + 1).Append(": ").Append(choice[i]).Append('\n');
            }

            var top = choice.GroupBy(x => x)
                .OrderByDescending(xg => xg.Count())
                .GroupBy(xg => xg.Count())
                .OrderByDescending(xg => xg.Key)
                .First();

            var topc = top.First().First().Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere");
            if (top.Count() > 1)
            {
                // tie-breaker
                var tie = top.Select(xg => xg.Key);
                topc = tie.ElementAt(this.RNG.Next(0, tie.Count()));
                sb.Append("Tie-breaker: ").Append(topc).Append('\n');
            }

            sb.Append("\nResult:\n").Append(topc);
            await ctx.RespondAsync(sb.ToString());
        }

        [Command("dice"), Description("Roll dice!")]
        public async Task DiceAsync(CommandContext ctx, [Description("Dies to roll, in xdy format (e.g. 2d20 or d6).")] string dice = "1d6")
        {
            if (string.IsNullOrWhiteSpace(dice))
                throw new ArgumentNullException(nameof(dice), "You need to specify what dice to roll.");

            await ctx.TriggerTypingAsync();

            var m = this.DiceRegex.Match(dice);
            if (!m.Success)
                throw new ArgumentNullException(nameof(dice), "You need to specify valid dice format.");

            var count = 1;
            if (m.Groups["count"].Success && !int.TryParse(m.Groups["count"].Value, out count))
                throw new ArgumentException("Invalid dice count specified", nameof(dice));

            if (count < 1 || count > 100)
                throw new ArgumentOutOfRangeException(nameof(dice), "Dice count needs to be greater than zero and less than or equal to 100.");

            if (!int.TryParse(m.Groups["sides"].Value, out var sides))
                throw new ArgumentException("Invalid side count specified", nameof(dice));

            var results = new int[count];
            for (var i = 0; i < count; i++)
                results[i] = this.RNG.Next(1, sides + 1);

            var resstr = string.Join(" ", results);
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":game_die:")} Results: {resstr}");
        }

        [Command("pp"), Description("Are you ready to learn the truth?")]
        public async Task PPAsync(CommandContext ctx)
        {
            await ctx.RespondAsync($"Your PP size (inches): {Derive(ctx.User.Id)}");

            static int Derive(ulong id)
            {
                Span<byte> src = stackalloc byte[sizeof(ulong) * 2];
                BinaryPrimitives.WriteUInt64LittleEndian(src, id);
                BinaryPrimitives.WriteUInt64BigEndian(src[sizeof(ulong)..], id);

                using var hash = SHA384.Create();
                Span<byte> dst = stackalloc byte[hash.HashSize / 8];
                hash.TryComputeHash(src, dst, out _);

                return (int)(BinaryPrimitives.ReadUInt64BigEndian(dst[^(sizeof(ulong))..]) % 13);
            }
        }
    }
}
