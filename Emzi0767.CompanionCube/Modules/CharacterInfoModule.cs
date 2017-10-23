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

namespace Emzi0767.CompanionCube.Modules
{
    [Group("charinfo"), Aliases("utfinfo", "unicodeinfo", "chrinfo", "ucinfo"), Description("Provides commands for obtaining information about unicode strings."), NotBlocked]
    public class CharacterInfoModule
    {
        private static Regex SeparatorReplacementRegex { get; } = new Regex(@"[\s\-,]");

        [Command("simple"), Aliases("short"), Description("Displays simplified unicode character information.")]
        public async Task SimpleAsync(CommandContext ctx, [RemainingText, Description("String to analyze.")] string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException("You need to supply a non-null string.");

            var cps = text.ToCodepoints().Select(xcp => string.Concat("`0x", xcp.Codepoint, "` (", xcp.UnihanData.IsUnihan ? xcp.UnihanData.Definition ?? xcp.Name : xcp.Name, ") - ", Formatter.Sanitize(xcp.CodepointString), " - <http://www.fileformat.info/info/unicode/char/", xcp.Codepoint, ">"));
            var pgs = new List<Page>();
            var pn = 1;
            var sb = new StringBuilder();
            foreach (var xcp in cps)
            {
                if (sb.Length + xcp.Length > 1000)
                {
                    sb.Append("\nPage: ").Append(pn++.ToString("#,##0")).Append(" of ");
                    pgs.Add(new Page { Content = sb.ToString() });
                    sb = new StringBuilder();
                }

                sb.Append(xcp).Append("\n");
            }

            sb.Append("\nPage: ").Append(pn++).Append(" of ");
            pgs.Add(new Page { Content = sb.ToString() });

            foreach (var xpg in pgs)
                xpg.Content = string.Concat(xpg.Content, pgs.Count.ToString("#,##0"));

            if (pgs.Count == 1)
            {
                var cnt = pgs.First().Content;
                cnt = cnt.Substring(0, cnt.LastIndexOf("\n\n"));
                await ctx.RespondAsync(cnt).ConfigureAwait(false);
            }
            else
            {
                var interact = ctx.Client.GetInteractivityModule();
                await interact.SendPaginatedMessage(ctx.Channel, ctx.User, pgs).ConfigureAwait(false);
            }
        }

        [Command("detailed"), Aliases("long", "full"), Description("Displays detailed unicode character information.")]
        public async Task DetailedAsync(CommandContext ctx, [RemainingText, Description("String to analyze. Up to 32 codepoints.")] string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException("You need to supply a non-null string.");

            var cps = text.ToCodepoints().ToArray();
            if (cps.Length > 32)
                throw new ArgumentException("You can only specify up to 32 codepoints.");

            var pgs = new List<Page>();
            for (var i = 0; i < cps.Length; i++)
            {
                var xcp = cps[i];
                var embed = new DiscordEmbedBuilder
                {
                    Title = string.Concat("`", xcp.Codepoint, "` - ", xcp.UnihanData.IsUnihan ? xcp.UnihanData.Definition ?? xcp.Name : xcp.Name),
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
                    embed.AddField("Unihan", "This is a Unihan character");

                if (!string.IsNullOrWhiteSpace(xcp.OldUnicodeName))
                    embed.AddField("Unicode 1.0 Name", xcp.OldUnicodeName);

                if (xcp.Decomposition.Codepoints.Any())
                {
                    var dcps = xcp.Decomposition.Codepoints.Select(xxcp => string.Concat("`", xxcp.Codepoint, "` (", xxcp.Name, ") = ", xxcp.CodepointString));
                    embed.AddField("Decomposition Type", xcp.Decomposition.Type.ToDescription())
                        .AddField("Decomposes Into", string.Join("\n", dcps));
                }

                if (xcp.NumericValue != null)
                {
                    var sb = new StringBuilder();
                    if (xcp.NumericValue?.Decimal != null)
                        sb.Append("Decimal: ").Append(xcp.NumericValue?.Decimal?.ToString("#,##0")).Append("\n");

                    if (xcp.NumericValue?.Digit != null)
                        sb.Append("Digit: ").Append(xcp.NumericValue?.Digit?.ToString("#,##0")).Append("\n");

                    if (xcp.NumericValue?.Numeric != null)
                        sb.Append("Numeric: ").Append(xcp.NumericValue?.Numeric).Append("\n");

                    if (sb.Length > 0)
                        embed.AddField("Numerical Values", sb.ToString());
                }

                if (xcp.SimpleUppercaseMapping.Codepoint != null)
                {
                    var ucm = xcp.SimpleUppercaseMapping;
                    embed.AddField("Uppercase Mapping", string.Concat("`", ucm.Codepoint, "` (", ucm.Name, ") = ", ucm.CodepointString));
                }

                if (xcp.SimpleLowercaseMapping.Codepoint != null)
                {
                    var lcm = xcp.SimpleLowercaseMapping;
                    embed.AddField("Lowercase Mapping", string.Concat("`", lcm.Codepoint, "` (", lcm.Name, ") = ", lcm.CodepointString));
                }

                if (xcp.SimpleTitlecaseMapping.Codepoint != null)
                {
                    var tcm = xcp.SimpleTitlecaseMapping;
                    if (tcm.Codepoint == xcp.SimpleUppercaseMapping.Codepoint)
                        embed.AddField("Titlecase Mapping", "Same as uppercase", false);
                    else
                        embed.AddField("Titlecase Mapping", string.Concat("`", tcm.Codepoint, "` (", tcm.Name, ") = ", tcm.CodepointString));
                }
                else if (xcp.SimpleUppercaseMapping.Codepoint != null)
                {
                    embed.AddField("Titlecase Mapping", "Same as uppercase");
                }

                pgs.Add(new Page { Embed = embed.Build() });
            }

            if (pgs.Count == 1)
            {
                await ctx.RespondAsync(embed: pgs.First().Embed).ConfigureAwait(false);
            }
            else
            {
                var interact = ctx.Client.GetInteractivityModule();
                await interact.SendPaginatedMessage(ctx.Channel, ctx.User, pgs).ConfigureAwait(false);
            }
        }
    }
}
