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

using System.Collections.Generic;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

namespace Emzi0767.CompanionCube
{
    public sealed class CompanionCubeHelpFormatter : BaseHelpFormatter
    {
        private DefaultHelpFormatter _d;

        public CompanionCubeHelpFormatter(CommandContext ctx)
            : base(ctx)
        {
            this._d = new DefaultHelpFormatter(ctx);
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            return this._d.WithCommand(command);
        }
        
        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            return this._d.WithSubcommands(subcommands);
        }

        public override CommandHelpMessage Build()
        {
            var hmsg = this._d.Build();
            var embed = new DiscordEmbedBuilder(hmsg.Embed)
            {
                Color = new DiscordColor(0xD091B2)
            };
            return new CommandHelpMessage(embed: embed);
        }
    }
}
