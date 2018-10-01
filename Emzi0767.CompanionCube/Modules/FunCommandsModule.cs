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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Emzi0767.CompanionCube.Attributes;
using Emzi0767.CompanionCube.Services;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("fun")]
    [Description("Commands for fun and great justice.")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [NotBlacklisted]
    public class FunCommandsModule : BaseCommandModule
    {
        private CSPRNG RNG { get; }
        private Regex DiceRegex { get; } = new Regex(@"^(?<count>\d+)?d(?<sides>\d+)$", RegexOptions.Compiled | RegexOptions.ECMAScript);

        public FunCommandsModule(CSPRNG rng)
        {
            this.RNG = rng;
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
                sb.Append("Choice ").Append(i + 1).Append(": ").Append(choice[i]).Append("\n");
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
                sb.Append("Tie-breaker: ").Append(topc).Append("\n");
            }

            sb.Append("\nResult:\n").Append(topc);
            await ctx.RespondAsync(sb.ToString());
        }

        [Command("dice"), Description("Roll dice!")]
        public async Task DiceAsync(CommandContext ctx, [Description("Dies to roll, in xdy format (e.g. 2d20 or d6).")] string dice = "1d6")
        {
            if (string.IsNullOrWhiteSpace(dice))
                throw new ArgumentNullException("You need to specify what dice to roll.");

            await ctx.TriggerTypingAsync();

            var m = this.DiceRegex.Match(dice);
            if (!m.Success)
                throw new ArgumentNullException("You need to specify valid dice format.");

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
    }
}