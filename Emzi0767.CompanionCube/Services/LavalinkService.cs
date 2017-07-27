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
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using Emzi0767.CompanionCube.Data;
using Emzi0767.Utilities;
using Microsoft.Extensions.Logging;

namespace Emzi0767.CompanionCube.Services
{
    /// <summary>
    /// Lavalink service which maintains a Lavalink node connection.
    /// </summary>
    public sealed class LavalinkService
    {
        public static EventId LogEvent { get; } = new EventId(1001, "LL-CCube");

        /// <summary>
        /// Gets the Lavalink node connection.
        /// </summary>
        public LavalinkNodeConnection LavalinkNode { get; private set; }

        private CompanionCubeConfigLavalink Configuration { get; }
        private DiscordClient Discord { get; }

        private readonly AsyncEvent<LavalinkGuildConnection, TrackExceptionEventArgs> _trackException;

        /// <summary>
        /// Creates a new Lavalink service with specified configuration options.
        /// </summary>
        /// <param name="cfg">Lavalink configuration.</param>
        /// <param name="client">Discord client to which the Lavalink will be attached.</param>
        public LavalinkService(CompanionCubeConfigLavalink cfg, DiscordClient client)
        {
            this.Configuration = cfg;
            this.Discord = client;
            this.Discord.Ready += this.Client_Ready;
            this._trackException = new AsyncEvent<LavalinkGuildConnection, TrackExceptionEventArgs>("CCUBE_LAVALINK_TRACK_EXCEPTION", TimeSpan.Zero, this.EventExceptionHandler);
        }

        private Task Client_Ready(DiscordClient sender, ReadyEventArgs e)
        {
            if (this.LavalinkNode == null)
                _ = Task.Run(async () =>
                {
                    var lava = sender.GetLavalink();
                    this.LavalinkNode = await lava.ConnectAsync(new LavalinkConfiguration
                    {
                        Password = this.Configuration.Password,

                        SocketEndpoint = new ConnectionEndpoint(this.Configuration.Hostname, this.Configuration.Port),
                        RestEndpoint = new ConnectionEndpoint(this.Configuration.Hostname, this.Configuration.Port)
                    });

                    this.LavalinkNode.TrackException += this.LavalinkNode_TrackException;
                });

            return Task.CompletedTask;
        }

        private async Task LavalinkNode_TrackException(LavalinkGuildConnection con, TrackExceptionEventArgs e)
        {
            await this._trackException.InvokeAsync(con, e);
        }

        public event AsyncEventHandler<LavalinkGuildConnection, TrackExceptionEventArgs> TrackExceptionThrown
        {
            add => this._trackException.Register(value);
            remove => this._trackException.Unregister(value);
        }

        private void EventExceptionHandler(
            AsyncEvent<LavalinkGuildConnection, TrackExceptionEventArgs> asyncEvent, 
            Exception exception, 
            AsyncEventHandler<LavalinkGuildConnection, TrackExceptionEventArgs> handler, 
            LavalinkGuildConnection sender, 
            TrackExceptionEventArgs eventArgs)
            => this.Discord.Logger.LogError(LogEvent, exception, "Exception occured during track playback");
    }
}
