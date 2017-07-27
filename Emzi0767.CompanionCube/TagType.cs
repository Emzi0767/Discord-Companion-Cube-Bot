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

using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using dse = DSharpPlus.Entities;

namespace Emzi0767.CompanionCube
{
    public enum TagType
    {
        Channel,
        Guild
    }

    public sealed class TagTypeConverter : IArgumentConverter<TagType>
    {
        public Task<dse.Optional<TagType>> ConvertAsync(string value, CommandContext ctx)
        {
            value = value.ToLowerInvariant();
            switch (value)
            {
                case "channel":
                    return Task.FromResult(dse.Optional.FromValue(TagType.Channel));

                case "guild":
                    return Task.FromResult(dse.Optional.FromValue(TagType.Guild));

                default:
                    return Task.FromResult(dse.Optional.FromNoValue<TagType>());
            }
        }
    }
}
