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

/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Emzi0767.CompanionCube.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("fun"), Description("Commands for fun and great justice."), NotBlocked]
    public class FunCommandsModule
    {
        private DatabaseClient Database { get; }
        private SharedData Shared { get; }

        public FunCommandsModule(DatabaseClient database, SharedData shared)
        {
            this.Database = database;
            this.Shared = shared;
        }

        [Command("needsmorejpeg"), Aliases("jpeg", "jpg", "morejpeg", "jaypeg"), Description("When you need more JPEG.")]
        public async Task JpegAsync(CommandContext ctx, [RemainingText] string url = null)
        {
            if (string.IsNullOrWhiteSpace(url))
                url = ctx.Message.Attachments.FirstOrDefault()?.Url;

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("You need to specify an image URL or attach an image.");

            await ctx.TriggerTypingAsync();
            var uri = new Uri(url);
            var res = await this.Shared.Http.GetAsync(uri);
            var cnt = await res.Content.ReadAsByteArrayAsync();

            using (var img = Image.Load<Rgba32>(cnt))
            using (var ms = new MemoryStream())
            {
                if (img.Width > 400 || img.Height > 400)
                    img.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(400, 400) }));
                img.SaveAsJpeg(ms, new JpegEncoder { Quality = 1 });
                ms.Position = 0;

                await ctx.RespondWithFileAsync(ms, "jaypeg.jpg", "Do I look like I know what a jaypeg is?");
            }
        }
    }
}
*/