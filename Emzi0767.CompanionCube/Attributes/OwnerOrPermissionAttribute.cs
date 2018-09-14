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
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Emzi0767.CompanionCube.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class OwnerOrPermissionAttribute : CheckBaseAttribute
    {
        public Permissions Permissions { get; private set; }

        public OwnerOrPermissionAttribute(Permissions permissions)
        {
            this.Permissions = permissions;
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var app = ctx.Client.CurrentApplication;
            var me = ctx.Client.CurrentUser;

            if (app != null && ctx.User.Id == app.Owner.Id)
                return Task.FromResult(true);

            if (ctx.User.Id == me.Id)
                return Task.FromResult(true);

            var usr = ctx.Member;
            if (usr == null)
                return Task.FromResult(false);
            var pusr = ctx.Channel.PermissionsFor(usr);

            return Task.FromResult((pusr & this.Permissions) == this.Permissions);
        }
    }
}