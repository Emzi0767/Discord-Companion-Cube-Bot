using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Emzi0767.CompanionCube.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Emzi0767.CompanionCube
{
    public class MusicEnabledAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (ctx.Guild == null)
                return Task.FromResult(false);

            if (help)
                return Task.FromResult(true);

            var db = ctx.Services.GetService<DatabaseClient>();
            return db.GetMusicOptionAsync(ctx.Guild.Id);
        }
    }
}
