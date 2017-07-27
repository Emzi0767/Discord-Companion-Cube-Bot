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
using System.Linq;
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

            if (app != null && app.Owners.Contains(ctx.User))
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