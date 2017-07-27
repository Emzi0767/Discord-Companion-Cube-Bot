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
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using d = DSharpPlus.Entities;

namespace Emzi0767.CompanionCube
{
    public enum HansTool
    {
        Luger,
        Flammenwerfer
    }

    public class HansToolConverter : IArgumentConverter<HansTool>
    {
        public Task<d.Optional<HansTool>> ConvertAsync(string value, CommandContext ctx)
            => Task.FromResult(Enum.TryParse<HansTool>(value, true, out var h)
                ? d.Optional.FromValue(h)
                : d.Optional.FromNoValue<HansTool>());
    }
}
