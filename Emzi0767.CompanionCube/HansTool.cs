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
