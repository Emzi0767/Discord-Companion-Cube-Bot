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

        private async Task Client_Ready(DiscordClient sender, ReadyEventArgs e)
        {
            if (this.LavalinkNode != null)
                return;

            var lava = sender.GetLavalink();
            this.LavalinkNode = await lava.ConnectAsync(new LavalinkConfiguration
            {
                Password = this.Configuration.Password,

                SocketEndpoint = new ConnectionEndpoint(this.Configuration.Hostname, this.Configuration.Port),
                RestEndpoint = new ConnectionEndpoint(this.Configuration.Hostname, this.Configuration.Port)
            });

            this.LavalinkNode.TrackException += this.LavalinkNode_TrackException;
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
