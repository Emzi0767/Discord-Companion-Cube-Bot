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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Emzi0767.CompanionCube.Attributes;
using Emzi0767.CompanionCube.Data;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("charinfo")]
    [Aliases("utfinfo", "unicodeinfo", "chrinfo", "ucinfo")]
    [Description("Provides commands for obtaining information about unicode strings. Invoking without subcommand will display simplified character information.")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [NotBlacklisted]
    public class CharacterInfoModule : BaseCommandModule
    {
        private static Regex SeparatorReplacementRegex { get; } = new Regex(@"[\s\-,]");

        [GroupCommand]
        public Task ExecuteGroupAsync(CommandContext ctx, [RemainingText, Description("String to analyze.")] string text)
            => this.SimpleAsync(ctx, text);

        [Command("simple"), Aliases("short"), Description("Displays simplified unicode character information.")]
        public async Task SimpleAsync(CommandContext ctx, [RemainingText, Description("String to analyze.")] string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException(nameof(text), "You need to supply a non-null string.");

            var cps = text.ToCodepoints().Select(xcp => string.Concat("`U+", xcp.Codepoint, "` (", xcp.Name, xcp.UnihanData.IsUnihan ? string.Concat(" / ", xcp.UnihanData.Definition) : "", ") - ", Formatter.Sanitize(xcp.CodepointString), " - <http://www.fileformat.info/info/unicode/char/", xcp.Codepoint, ">"));

            var pages = new List<StringBuilder>(cps.Sum(x => x.Length) / 1000 + 1);
            var sb = new StringBuilder();
            var pn = 1;
            foreach (var xcp in cps)
            {
                if (sb.Length + xcp.Length > 1024)
                {
                    sb.Append("\nPage: ").Append(pn++).Append(" of ");
                    pages.Add(sb);
                    sb = new StringBuilder();
                }

                sb.Append(xcp).Append('\n');
            }

            if (pn != 1)
            {
                sb.Append("\nPage: ").Append(pn).Append(" of ");
                pages.Add(sb);

                var pga = pages.Select(x => new Page(x.Append(pn).ToString(), null)).ToArray();
                var ems = new PaginationEmojis
                {
                    SkipLeft = null,
                    SkipRight = null,
                    Stop = DiscordEmoji.FromUnicode("⏹"),
                    Left = DiscordEmoji.FromUnicode("◀"),
                    Right = DiscordEmoji.FromUnicode("▶")
                };

                var interact = ctx.Client.GetInteractivity();
                await interact.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pga, ems, PaginationBehaviour.Ignore, PaginationDeletion.KeepEmojis);
            }
            else
            {
                var cnt = sb.ToString();
                await ctx.RespondAsync(cnt);
            }
        }

        [Command("detailed"), Aliases("long", "full"), Description("Displays detailed unicode character information.")]
        public async Task DetailedAsync(CommandContext ctx, [RemainingText, Description("String to analyze. Up to 32 codepoints.")] string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException(nameof(text), "You need to supply a non-null string.");

            var cps = text.ToCodepoints().ToArray();
            if (cps.Length > 32)
                throw new ArgumentException("You can only specify up to 32 codepoints.", nameof(text));

            var pgs = new List<DiscordEmbedBuilder>();
            for (var i = 0; i < cps.Length; i++)
            {
                var xcp = cps[i];
                var embed = new DiscordEmbedBuilder
                {
                    Title = string.Concat("`U+", xcp.Codepoint, "` - ", xcp.Name),
                    Color = new DiscordColor(0xD091B2),
                    // they watermark
                    //ThumbnailUrl = string.Concat("http://www.fileformat.info/info/unicode/char/", xcp.Codepoint.ToLower(), "/", SeparatorReplacementRegex.Replace(xcp.Name, "_").ToLower(), ".png"),
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = string.Concat("Page ", (i + 1).ToString("#,##0"), " of ", cps.Length.ToString("#,##0"))
                    }
                };

                if (!string.IsNullOrWhiteSpace(xcp.CodepointString))
                    embed.Description = xcp.CodepointString;

                embed.AddField("Unicode Block", xcp.Block.Name)
                    .AddField("Unicode Category", xcp.Category.ToDescription())
                    .AddField("Combining Class", xcp.CombiningClass.ToDescription())
                    .AddField("Bidirectionality Class", xcp.BidirectionalClass.ToDescription())
                    .AddField("Bidirectional Mirrored?", xcp.BidirectionalMirrored ? "Yes" : "No");

                if (xcp.UnihanData.IsUnihan)
                    embed.AddField("Unihan", string.Concat("This is a Unihan character\nDefinition: ", xcp.UnihanData.Definition));

                if (!string.IsNullOrWhiteSpace(xcp.OldUnicodeName))
                    embed.AddField("Unicode 1.0 Name", xcp.OldUnicodeName);

                if (xcp.Decomposition.Codepoints.Any())
                {
                    var dcps = xcp.Decomposition.Codepoints.Select(xxcp => string.Concat("`U+", xxcp.Codepoint, "` (", xxcp.Name, ") = ", xxcp.CodepointString));
                    embed.AddField("Decomposition Type", xcp.Decomposition.Type.ToDescription())
                        .AddField("Decomposes Into", string.Join("\n", dcps));
                }

                if (xcp.NumericValue != null)
                {
                    var sb = new StringBuilder();
                    if (xcp.NumericValue?.Decimal != null)
                        sb.Append("Decimal: ").Append(xcp.NumericValue?.Decimal?.ToString("#,##0")).Append('\n');

                    if (xcp.NumericValue?.Digit != null)
                        sb.Append("Digit: ").Append(xcp.NumericValue?.Digit?.ToString("#,##0")).Append('\n');

                    if (xcp.NumericValue?.Numeric != null)
                        sb.Append("Numeric: ").Append(xcp.NumericValue?.Numeric).Append('\n');

                    if (sb.Length > 0)
                        embed.AddField("Numerical Values", sb.ToString());
                }

                if (xcp.SimpleUppercaseMapping.Codepoint != null)
                {
                    var ucm = xcp.SimpleUppercaseMapping;
                    embed.AddField("Uppercase Mapping", string.Concat("`U+", ucm.Codepoint, "` (", ucm.Name, ") = ", ucm.CodepointString));
                }

                if (xcp.SimpleLowercaseMapping.Codepoint != null)
                {
                    var lcm = xcp.SimpleLowercaseMapping;
                    embed.AddField("Lowercase Mapping", string.Concat("`U+", lcm.Codepoint, "` (", lcm.Name, ") = ", lcm.CodepointString));
                }

                if (xcp.SimpleTitlecaseMapping.Codepoint != null)
                {
                    var tcm = xcp.SimpleTitlecaseMapping;
                    if (tcm.Codepoint == xcp.SimpleUppercaseMapping.Codepoint)
                        embed.AddField("Titlecase Mapping", "Same as uppercase", false);
                    else
                        embed.AddField("Titlecase Mapping", string.Concat("`U+", tcm.Codepoint, "` (", tcm.Name, ") = ", tcm.CodepointString));
                }
                else if (xcp.SimpleUppercaseMapping.Codepoint != null)
                {
                    embed.AddField("Titlecase Mapping", "Same as uppercase");
                }

                pgs.Add(embed);
            }

            if (pgs.Count == 1)
            {
                await ctx.RespondAsync(embed: pgs.First());
            }
            else
            {
                var pga = pgs.Select(x => new Page(null, x)).ToArray();
                var ems = new PaginationEmojis
                {
                    SkipLeft = null,
                    SkipRight = null,
                    Stop = DiscordEmoji.FromUnicode("⏹"),
                    Left = DiscordEmoji.FromUnicode("◀"),
                    Right = DiscordEmoji.FromUnicode("▶")
                };

                var interact = ctx.Client.GetInteractivity();
                await interact.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pga, ems, PaginationBehaviour.Ignore, PaginationDeletion.KeepEmojis);
            }
        }
    }
}
