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
