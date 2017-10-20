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
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Emzi0767.CompanionCube.Services;

namespace Emzi0767.CompanionCube
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class NotBlockedAttribute : CheckBaseAttribute
    {
        public override Task<bool> CanExecute(CommandContext ctx, bool help)
        {
            var shared = ctx.Dependencies.GetDependency<SharedData>();

            if (shared.BlockedUsers.Contains(ctx.User.Id))
                return Task.FromResult(false);
            
            if (shared.BlockedChannels.Contains(ctx.Channel.Id))
                return Task.FromResult(false);
            
            return Task.FromResult(true);
        }
    }
}