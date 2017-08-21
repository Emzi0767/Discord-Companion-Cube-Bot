using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;

namespace Emzi0767.CompanionCube
{
    public sealed class CompanionCubeHelpFormatter : IHelpFormatter
    {
        private DefaultHelpFormatter _d;

        public CompanionCubeHelpFormatter()
        {
            this._d = new DefaultHelpFormatter();
        }

        public IHelpFormatter WithCommandName(string name)
        {
            return this._d.WithCommandName(name);
        }

        public IHelpFormatter WithDescription(string description)
        {
            return this._d.WithDescription(description);
        }

        public IHelpFormatter WithGroupExecutable()
        {
            return this._d.WithGroupExecutable();
        }

        public IHelpFormatter WithAliases(IEnumerable<string> aliases)
        {
            return this._d.WithAliases(aliases);
        }

        public IHelpFormatter WithArguments(IEnumerable<CommandArgument> arguments)
        {
            return this._d.WithArguments(arguments);
        }

        public IHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            return this._d.WithSubcommands(subcommands);
        }

        public CommandHelpMessage Build()
        {
            var hmsg = this._d.Build();
            var embed = new DiscordEmbedBuilder(hmsg.Embed)
            {
                Color = new DiscordColor(0xD091B2)
            };
            return new CommandHelpMessage(embed: embed);
        }
    }
}
