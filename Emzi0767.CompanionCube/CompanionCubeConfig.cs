// This file is part of Emzi0767.CompanionCube project
//
// Copyright 2017 Emzi0767
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
using Newtonsoft.Json;

namespace Emzi0767.CompanionCube
{
    public struct CompanionCubeConfig
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("default_prefix")]
        public string DefaultCommandPrefix { get; private set; }

        [JsonProperty("enable_mention_prefix")]
        public bool EnableMentionPrefix { get; private set; }

        [JsonProperty("message_cache_size")]
        public int MessageCacheSize { get; private set; }

        [JsonProperty("shard_count")]
        public int ShardCount { get; private set; }

        [JsonProperty("game")]
        public string Game { get; private set; }

        [JsonProperty("currency_symbol")]
        public string CurrencySymbol { get; private set; }

        [JsonProperty("database_config")]
        public CompanionCubeDatabaseConfig DatabaseConfig { get; private set; }

        [JsonIgnore]
        public static CompanionCubeConfig Default
        {
            get
            {
                return new CompanionCubeConfig
                {
                    Token = "<insert token here>",
                    DefaultCommandPrefix = "cc!",
                    EnableMentionPrefix = true,
                    MessageCacheSize = 50,
                    ShardCount = 1,
                    Game = "with Portals",
                    CurrencySymbol = "<:comedy_chevron:338403292138962944>",
                    DatabaseConfig = CompanionCubeDatabaseConfig.Default
                };
            }
        }
    }

    public struct CompanionCubeDatabaseConfig
    {
        [JsonProperty("hostname")]
        public string Hostname { get; private set; }

        [JsonProperty("port")]
        public int Port { get; private set; }

        [JsonProperty("database")]
        public string Database { get; private set; }

        [JsonProperty("username")]
        public string Username { get; private set; }

        [JsonProperty("password")]
        public string Password { get; private set; }

        [JsonProperty("table_name_prefix")]
        public string TableNamePrefix { get; private set; }

        [JsonIgnore]
        public static CompanionCubeDatabaseConfig Default
        {
            get 
            {
                return new CompanionCubeDatabaseConfig 
                {
                    Hostname = "localhost",
                    Port = 5432,
                    Database = "companion_cube",
                    Username = "companion_cube",
                    Password = "ebuc_noinapmoc",
                    TableNamePrefix = "cc_"
                }; 
            }
        }
    }
}