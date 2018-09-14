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

using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Net.Udp;
using Emzi0767.CompanionCube.Data;

namespace Emzi0767.CompanionCube.Services
{
    /// <summary>
    /// Lavalink service which maintains a Lavalink node connection.
    /// </summary>
    public sealed class LavalinkService
    {
        /// <summary>
        /// Gets the Lavalink node connection.
        /// </summary>
        public LavalinkNodeConnection LavalinkNode { get; private set; }

        private CompanionCubeConfigLavalink Configuration { get; }

        /// <summary>
        /// Creates a new Lavalink service with specified configuration options.
        /// </summary>
        /// <param name="cfg">Lavalink configuration.</param>
        /// <param name="client">Discord client to which the Lavalink will be attached.</param>
        public LavalinkService(CompanionCubeConfigLavalink cfg, DiscordClient client)
        {
            this.Configuration = cfg;
            client.Ready += this.Client_Ready;
        }

        private async Task Client_Ready(ReadyEventArgs e)
        {
            if (this.LavalinkNode != null)
                return;

            var lava = e.Client.GetLavalink();
            this.LavalinkNode = await lava.ConnectAsync(new LavalinkConfiguration
            {
                Password = this.Configuration.Password,

                SocketEndpoint = new ConnectionEndpoint { Hostname = this.Configuration.Hostname, Port = this.Configuration.WebSocketPort },
                RestEndpoint = new ConnectionEndpoint { Hostname = this.Configuration.Hostname, Port = this.Configuration.RestPort }
            }).ConfigureAwait(false);
        }
    }
}
