using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Emzi0767.CompanionCube
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class OwnerOrPermissionAttribute : CheckBaseAttribute
    {
        public Permissions Permissions { get; private set; }

        public OwnerOrPermissionAttribute(Permissions permissions)
        {
            this.Permissions = permissions;
        }

        public override async Task<bool> CanExecute(CommandContext ctx)
        {
            var app = ctx.Client.CurrentApplication;
            var me = ctx.Client.CurrentUser;

            if (app != null && ctx.User.Id == app.Owner.Id)
                return true;

            if (ctx.User.Id == me.Id)
                return true;

            var usr = ctx.Member;
            if (usr == null)
                return false;
            var pusr = ctx.Channel.PermissionsFor(usr);

            var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
            if (bot == null)
                return false;
            var pbot = ctx.Channel.PermissionsFor(bot);

            if ((pusr & this.Permissions) == this.Permissions && (pbot & this.Permissions) == this.Permissions)
                return true;

            return false;
        }
    }
}