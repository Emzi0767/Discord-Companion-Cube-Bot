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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Emzi0767.CompanionCube.Attributes;
using Microsoft.Extensions.PlatformAbstractions;

namespace Emzi0767.CompanionCube.Modules
{
    [ModuleLifespan(ModuleLifespan.Transient)]
    [NotBlacklisted]
    public sealed class MiscCommandsModule : BaseCommandModule
    {
        public MiscCommandsModule()
        { }

        [Command("about"), Aliases("info"), Description("Displays information about the bot.")]
        public async Task AboutAsync(CommandContext ctx)
        {
            var ccv = typeof(CompanionCubeBot)
                .GetTypeInfo()
                .Assembly
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ??

                typeof(CompanionCubeBot)
                .GetTypeInfo()
                .Assembly
                .GetName()
                .Version
                .ToString(3);
            
            var dsv = ctx.Client.VersionString;
            var ncv = PlatformServices.Default
                .Application
                .RuntimeFramework
                .Version
                .ToString(2);
            
            try
            {
                var a = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(xa => xa.GetName().Name == "System.Private.CoreLib");
                var pth = Path.GetDirectoryName(a.Location);
                pth = Path.Combine(pth, ".version");
                using (var fs = File.OpenRead(pth))
                using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                {
                    await sr.ReadLineAsync();
                    ncv = await sr.ReadLineAsync();
                }
            }
            catch { }

            var invuri = string.Concat("https://discordapp.com/oauth2/authorize?scope=bot&permissions=0&client_id=", ctx.Client.CurrentApplication.Id);

            var embed = new DiscordEmbedBuilder
            {
                Title = "About Companion Cube",
                Url = "https://emzi0767.com/Discord/CompanionCube",
                Description = string.Concat("Companion Cube is a bot made by Emzi0767#1837 (<@!181875147148361728>). The source code is available on ", Formatter.MaskedUrl("Emzi's GitHub",
                        new Uri("https://github.com/Emzi0767/Discord-Companion-Cube-Bot"), "Companion Cube's source code on GitHub"),
                    ".\n\nThis shard is currently servicing ", ctx.Client.Guilds.Count.ToString("#,##0"),
                    " guilds.\n\nClick ", Formatter.MaskedUrl("this invite link", new Uri(invuri), "Companion Cube invite link"), " to invite me to your guild!"),
                Color = new DiscordColor(0xD091B2)
            };

            embed.AddField("Bot Version", string.Concat(DiscordEmoji.FromName(ctx.Client, ":companion_cube:"), " ", Formatter.Bold(ccv)), true)
                .AddField("DSharpPlus Version", string.Concat(DiscordEmoji.FromName(ctx.Client, ":dsharpplus:"), " ", Formatter.Bold(dsv)), true)
                .AddField(".NET Core Version", string.Concat(DiscordEmoji.FromName(ctx.Client, ":dotnet:"), " ", Formatter.Bold(ncv)), true);

            await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("uptime"), Description("Display bot's uptime.")]
        public async Task UptimeAsync(CommandContext ctx)
        {
            var upt = DateTime.Now - Process.GetCurrentProcess().StartTime;
            var ups = upt.ToDurationString();
            await ctx.RespondAsync(string.Concat("\u200b", DiscordEmoji.FromName(ctx.Client, ":companion_cube:"), " The bot has been running for ", Formatter.Bold(ups), ".")).ConfigureAwait(false);
        }

        [Command("ping"), Description("Displays this shard's WebSocket latency.")]
        public async Task PingAsync(CommandContext ctx)
        {
            await ctx.RespondAsync(string.Concat("\u200b", DiscordEmoji.FromName(ctx.Client, ":ping_pong:"), " WebSocket latency: ", ctx.Client.Ping.ToString("#,##0"), "ms.")).ConfigureAwait(false);
        }

        [Command("cleanup")]
        public async Task CleanupAsync(CommandContext ctx, [Description("Maximum number of messages to clean up.")] int max_count = 100)
        {
            var lid = 0ul;
            for (var i = 0; i < max_count; i += 100)
            {
                var msgs = await ctx.Channel.GetMessagesBeforeAsync(lid != 0 ? lid : ctx.Message.Id, Math.Min(max_count - i, 100)).ConfigureAwait(false);
                var msgsf = msgs.Where(xm => xm.Author.Id == ctx.Client.CurrentUser.Id).OrderBy(xm => xm.Id);

                var lmsg = msgsf.FirstOrDefault();
                if (lmsg == null)
                    break;

                lid = lmsg.Id;

                try
                {
                    await ctx.Channel.DeleteMessagesAsync(msgsf).ConfigureAwait(false);
                }
                catch (UnauthorizedException)
                {
                    foreach (var xmsg in msgsf)
                        await xmsg.DeleteAsync();
                }
            }

            var msg = await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":msokhand:").ToString()).ConfigureAwait(false);
            await Task.Delay(2500).ContinueWith(t => msg.DeleteAsync());
        }

        [Group("emoji")]
        [Aliases("emotes", "emote", "emojis")]
        [Description("Commands for managing emoji.")]
        [ModuleLifespan(ModuleLifespan.Transient)]
        [OwnerOrPermission(Permissions.ManageEmojis)]
        public class Emoji : BaseCommandModule
        {
            public HttpClient Http { get; }

            public Emoji(HttpClient http)
            {
                this.Http = http;
            }

            [Command("steal"), Description("Installs specified emote on current server.")]
            public async Task StealAsync(CommandContext ctx, DiscordEmoji emoji, string name)
            {
                if (emoji.Id == 0)
                    throw new InvalidOperationException("Cannot steal a unicode emoji.");

                DiscordGuildEmoji nemoji = null;
                using (var res = await this.Http.GetAsync(emoji.Url).ConfigureAwait(false))
                using (var rss = await res.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    nemoji = await ctx.Guild.CreateEmojiAsync(name, rss, reason: $"{ctx.User.Username}#{ctx.User.Discriminator} ({ctx.User.Id}) stole a meme.").ConfigureAwait(false);

                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} {nemoji}").ConfigureAwait(false);
            }

            [Command("install"), Description("Installs specified image as emote in this server.")]
            public async Task InstallAsync(CommandContext ctx, string name, [RemainingText] string url = null)
            {
                url = string.IsNullOrWhiteSpace(url) ? null : url;

                if (url == null && !ctx.Message.Attachments.Any())
                    throw new ArgumentNullException(nameof(url), "Need to specify a URL or add an attachment.");

                if (url == null)
                {
                    var att = ctx.Message.Attachments.First();
                    var fn = att.FileName.ToLowerInvariant();

                    if (!(fn.EndsWith(".png") || fn.EndsWith(".gif")))
                        throw new ArgumentException("Attachment needs to be a GIF or PNG image.", nameof(url));

                    url = att.Url;
                }

                DiscordGuildEmoji nemoji = null;
                using (var res = await this.Http.GetAsync(url).ConfigureAwait(false))
                using (var rss = await res.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    nemoji = await ctx.Guild.CreateEmojiAsync(name, rss, reason: $"{ctx.User.Username}#{ctx.User.Discriminator} ({ctx.User.Id}) installed a meme.").ConfigureAwait(false);

                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msokhand:")} {nemoji}").ConfigureAwait(false);
            }

            [Command("list"), Description("Lists all emotes in this server.")]
            public async Task ListAsync(CommandContext ctx)
            {
                var ems = ctx.Guild.Emojis;
                if (!ems.Any())
                {
                    await ctx.RespondAsync("There are no custom emotes in this server.");
                    return;
                }

                var stems = ems.Where(xe => !xe.IsAnimated);
                var anems = ems.Where(xe => xe.IsAnimated);
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Custom emotes in this server"
                };

                if (stems.Any())
                    embed.AddField("Regular emotes", string.Join(" ", stems), false);
                if (anems.Any())
                    embed.AddField("Animated emotes", string.Join(" ", anems), false);

                await ctx.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
            }
        }
    }
}