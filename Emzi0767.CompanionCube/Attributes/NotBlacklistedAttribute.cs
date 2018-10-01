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
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Emzi0767.CompanionCube.Data;
using Emzi0767.CompanionCube.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Emzi0767.CompanionCube.Attributes
{
    /// <summary>
    /// Verifies that the user is not blacklisted for the purpose of the command usage.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class NotBlacklistedAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (ctx.Guild == null)
                return Task.FromResult(false);

            if (help)
                return Task.FromResult(true);

            if (ctx.User == ctx.Client.CurrentApplication.Owner)
                return Task.FromResult(true);

            var uid = (long)ctx.User.Id;
            var cid = (long)ctx.Channel.Id;
            var gid = (long)ctx.Guild.Id;

            var db = ctx.Services.GetService<DatabaseContext>();
            var blocked = db.EntityBlacklist.Any(x => (x.Id == uid && x.Kind == DatabaseEntityKind.User) || (x.Id == cid && x.Kind == DatabaseEntityKind.Channel) || (x.Id == gid && x.Kind == DatabaseEntityKind.Guild));
            return Task.FromResult(!blocked);
        }
    }
}