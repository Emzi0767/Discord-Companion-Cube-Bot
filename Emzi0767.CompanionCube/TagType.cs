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

using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;

namespace Emzi0767.CompanionCube
{
    public enum TagType
    {
        Channel,
        Guild
    }

    public sealed class TagTypeConverter : IArgumentConverter<TagType>
    {
        public Task<Optional<TagType>> ConvertAsync(string value, CommandContext ctx)
        {
            value = value.ToLowerInvariant();
            switch (value)
            {
                case "channel":
                    return Task.FromResult(Optional<TagType>.FromValue(TagType.Channel));

                case "guild":
                    return Task.FromResult(Optional<TagType>.FromValue(TagType.Guild));

                default:
                    return Task.FromResult(Optional<TagType>.FromNoValue());
            }
        }
    }
}
