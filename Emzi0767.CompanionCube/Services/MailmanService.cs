using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Emzi0767.CompanionCube.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Emzi0767.CompanionCube.Services
{
    /// <summary>
    /// Provides mailman functionality.
    /// </summary>
    public sealed class MailmanService
    {
        private static Regex SourceChannelRegex { get; } = new Regex(@"^\[c:(?<channel>\d+)\]", RegexOptions.Compiled);

        private DiscordClient Discord { get; }
        private HttpClient Http { get; }

        private MailmanSettings Settings { get; set; } = null;
        private bool IsEnabled { get; set; } = true;
        private DiscordChannel Channel { get; set; } = null;

        public MailmanService(DiscordClient discord, HttpClient http)
        {
            this.Discord = discord;
            this.Discord.MessageCreated += this.Discord_MessageCreated;

            this.Http = http;
        }

        public async Task EnableAsync(DatabaseContext db, ulong guildId, ulong channelId)
        {
            var meta = await db.Metadata.FirstOrDefaultAsync(x => x.MetaKey == MailmanSettings.MetaKey);
            this.Settings = new MailmanSettings
            {
                Guild = guildId,
                Channel = channelId
            };
            this.Channel = await this.Discord.GetChannelAsync(this.Settings.Channel);
            var settingsJson = JsonConvert.SerializeObject(this.Settings);
            this.IsEnabled = true;

            if (meta != null)
            {
                meta.MetaValue = settingsJson;
                db.Metadata.Update(meta);
            }
            else
            {
                await db.Metadata.AddAsync(new DatabaseMetadata
                {
                    MetaKey = MailmanSettings.MetaKey,
                    MetaValue = settingsJson
                });
            }

            await db.SaveChangesAsync();
        }

        public async Task DisableAsync(DatabaseContext db)
        {
            this.Settings = null;
            this.IsEnabled = false;
            this.Channel = null;
            var meta = await db.Metadata.FirstOrDefaultAsync(x => x.MetaKey == MailmanSettings.MetaKey);
            if (meta != null)
            {
                db.Metadata.Remove(meta);
                await db.SaveChangesAsync();
            }
        }

        public MailmanSettings GetStatus()
            => this.Settings;

        public async Task<bool> SendMessageAsync(DatabaseContext db, DiscordUser author, DiscordChannel source, string message, IEnumerable<(string name, Uri uri, int length)> attachments)
        {
            await this.InitializeSettingsAsync(db);
            if (!this.IsEnabled)
                return false;

            var msg = !string.IsNullOrWhiteSpace(message)
                ? $"[c:{source.Id}] {author.Username}#{author.Discriminator} ({author.Id}):\n\n{message}"
                : $"[c:{source.Id}] {author.Username}#{author.Discriminator} ({author.Id})";

            var msgb = new DiscordMessageBuilder()
                .WithContent(msg)
                .WithAllowedMentions(Mentions.None);

            foreach (var (name, uri, length) in attachments)
                await this.AddFileAsync(msgb, name, uri, length);

            await this.Channel.SendMessageAsync(msgb);
            return true;
        }

        public async Task ForceInitializeAsync(DatabaseContext db)
            => await this.InitializeSettingsAsync(db);

        private async Task InitializeSettingsAsync(DatabaseContext db)
        {
            if (!this.IsEnabled || this.Settings is not null)
                return;

            var meta = await db.Metadata.FirstOrDefaultAsync(x => x.MetaKey == MailmanSettings.MetaKey);
            if (meta == null)
            {
                this.IsEnabled = false;
                this.Channel = null;
                return;
            }

            this.Settings = JsonConvert.DeserializeObject<MailmanSettings>(meta.MetaValue);
            this.Channel = await this.Discord.GetChannelAsync(this.Settings.Channel);
        }

        private async Task Discord_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (!this.IsEnabled || this.Settings is null /* not initalized */)
                return;

            if (e.Channel.Id != this.Settings.Channel)
                return;

            if (e.Message.ReferencedMessage == null)
                return;

            var refmsg = e.Message.ReferencedMessage;
            if (refmsg.Content == null)
                refmsg = await e.Channel.GetMessageAsync(refmsg.Id);

            var srcmatch = SourceChannelRegex.Match(refmsg.Content);
            if (!srcmatch.Success || !srcmatch.Groups["channel"].Success)
                return;

            var srcraw = srcmatch.Groups["channel"].Value;
            if (!ulong.TryParse(srcraw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var srcid))
                return;

            var src = await this.Discord.GetChannelAsync(srcid);

            var msg = !string.IsNullOrWhiteSpace(e.Message.Content)
                ? $"**Emzi:** {e.Message.Content}"
                : $"**Emzi**";

            var msgb = new DiscordMessageBuilder()
                .WithContent(msg)
                .WithAllowedMentions(Mentions.None);

            if (e.Message.Attachments != null)
                foreach (var attach in e.Message.Attachments)
                    await this.AddFileAsync(msgb, attach.FileName, new Uri(attach.Url), attach.FileSize);

            await src.SendMessageAsync(msgb);
        }

        private async Task AddFileAsync(DiscordMessageBuilder msgbuilder, string name, Uri uri, int length)
        {
            var ms = new MemoryStream(length); // don't need no disposing
            using var res = await this.Http.GetAsync(uri, HttpCompletionOption.ResponseContentRead);
            using var cnt = await res.Content.ReadAsStreamAsync();
            await cnt.CopyToAsync(ms);
            ms.Position = 0;

            msgbuilder.WithFile(name, ms);
        }
    }
}
