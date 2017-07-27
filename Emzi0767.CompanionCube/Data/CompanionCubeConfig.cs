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

using System.Collections.Immutable;
using Newtonsoft.Json;

namespace Emzi0767.CompanionCube.Data
{
    /// <summary>
    /// Represents the entire configuration file.
    /// </summary>
    public sealed class CompanionCubeConfig
    {
        /// <summary>
        /// Gets this configuration file's version data.
        /// </summary>
        [JsonProperty("version")]
        public CompanionCubeConfigVersion Version { get; private set; } = new CompanionCubeConfigVersion();

        /// <summary>
        /// Gets the Discord configuration.
        /// </summary>
        [JsonProperty("discord")]
        public CompanionCubeConfigDiscord Discord { get; private set; } = new CompanionCubeConfigDiscord();

        /// <summary>
        /// Gets the PostgreSQL configuration.
        /// </summary>
        [JsonProperty("postgres")]
        public CompanionCubeConfigPostgres PostgreSQL { get; private set; } = new CompanionCubeConfigPostgres();

        /// <summary>
        /// Gets the Lavalink configuration.
        /// </summary>
        [JsonProperty("lavalink")]
        public CompanionCubeConfigLavalink Lavalink { get; private set; } = new CompanionCubeConfigLavalink();

        /// <summary>
        /// Gets the YouTube API configuration.
        /// </summary>
        [JsonProperty("youtube")]
        public CompanionCubeConfigYouTube YouTube { get; private set; } = new CompanionCubeConfigYouTube();

        /// <summary>
        /// Gets the GitHub API configuration.
        /// </summary>
        [JsonProperty("github")]
        public CompanionCubeConfigGitHub GitHub { get; private set; } = new CompanionCubeConfigGitHub();
    }

    /// <summary>
    /// Represents version section of the configuration file.
    /// </summary>
    public sealed class CompanionCubeConfigVersion
    {
        /// <summary>
        /// Gets the numeric version of the config data.
        /// </summary>
        [JsonProperty("config")]
        public int Configuration { get; private set; }
    }

    /// <summary>
    /// Represents Discord section of the configuration file.
    /// </summary>
    public sealed class CompanionCubeConfigDiscord
    {
        /// <summary>
        /// Gets the token for Discord API.
        /// </summary>
        [JsonProperty("token")]
        public string Token { get; private set; } = "insert_token_here";

        /// <summary>
        /// Gets the default command prefixes of the bot.
        /// </summary>
        [JsonProperty("prefixes")]
        public ImmutableArray<string> DefaultPrefixes { get; private set; } = new[] { "cc!", "//", "??" }.ToImmutableArray();

        /// <summary>
        /// Gets whether to enable the user mention prefix for the bot.
        /// </summary>
        [JsonProperty("mention_prefix")]
        public bool EnableMentionPrefix { get; private set; } = true;

        /// <summary>
        /// Gets the size of the message cache. 0 means disable.
        /// </summary>
        [JsonProperty("message_cache_size")]
        public int MessageCacheSize { get; private set; } = 512;

        /// <summary>
        /// Gets the total number of shards on which the bot will operate.
        /// </summary>
        [JsonProperty("shards")]
        public int ShardCount { get; private set; } = 1;

        /// <summary>
        /// Gets the game the bot will be playing. Null means disable.
        /// </summary>
        [JsonProperty("game")]
        public string Game { get; private set; } = "with Portals";
    }

    /// <summary>
    /// Represents PostgreSQL section of the configuration file.
    /// </summary>
    public sealed class CompanionCubeConfigPostgres
    {
        /// <summary>
        /// Gets the hostname of the PostgreSQL server.
        /// </summary>
        [JsonProperty("hostname")]
        public string Hostname { get; private set; } = "localhost";

        /// <summary>
        /// Gets the port of the PostgreSQL server.
        /// </summary>
        [JsonProperty("port")]
        public int Port { get; private set; } = 5432;

        /// <summary>
        /// Gets the name of the PostgreSQL database.
        /// </summary>
        [JsonProperty("database")]
        public string Database { get; private set; } = "companion_cube";

        /// <summary>
        /// Gets the username for PostgreSQL authentication.
        /// </summary>
        [JsonProperty("username")]
        public string Username { get; private set; } = "companion_cube";

        /// <summary>
        /// Gets the password for PostgreSQL authentication.
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; private set; } = "hunter2";

        /// <summary>
        /// Gets whether to use SSL/TLS for communication with PostgreSQL server.
        /// </summary>
        [JsonProperty("ssl")]
        public bool UseEncryption { get; private set; } = true;

        /// <summary>
        /// Gets whether to trust the PostgreSQL server certificate unconditionally.
        /// </summary>
        [JsonProperty("trust_certificate")]
        public bool TrustServerCertificate { get; private set; } = true;

        /// <summary>
        /// Gets the required database schema version.
        /// </summary>
        [JsonProperty("schema_version")]
        public int SchemaVersion { get; private set; } = 4;
    }

    /// <summary>
    /// Represents Lavalink section of the configuration file.
    /// </summary>
    public sealed class CompanionCubeConfigLavalink
    {
        /// <summary>
        /// Gets the hostname of the Lavalink node server.
        /// </summary>
        [JsonProperty("hostname")]
        public string Hostname { get; private set; } = "localhost";

        /// <summary>
        /// Gets the port of the Lavalink node server.
        /// </summary>
        [JsonProperty("port")]
        public int Port { get; private set; } = 2333;

        /// <summary>
        /// Gets the password to Lavalink node server.
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; private set; } = "youshallnotpass";
    }

    /// <summary>
    /// Represents YouTube section of the configuration file.
    /// </summary>
    public sealed class CompanionCubeConfigYouTube
    {
        /// <summary>
        /// Gets the API key for YouTube's data API.
        /// </summary>
        [JsonProperty("api_key")]
        public string ApiKey { get; private set; } = "insert_api_key_here";
    }

    /// <summary>
    /// Represents GitHub section of the configuration file.
    /// </summary>
    public sealed class CompanionCubeConfigGitHub
    {
        /// <summary>
        /// Gets whether processing is enabled.
        /// </summary>
        [JsonProperty("enabled")]
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Gets the channels allowed to process Issues and PRs.
        /// </summary>
        [JsonProperty("channels")]
        public ulong[] Channels { get; private set; }

        /// <summary>
        /// Gets the guilds allowed to process Issues and PRs.
        /// </summary>
        [JsonProperty("guilds")]
        public ulong[] Guilds { get; private set; }
    }
}