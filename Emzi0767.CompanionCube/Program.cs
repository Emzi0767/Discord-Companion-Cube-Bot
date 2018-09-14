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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Emzi0767.CompanionCube.Data;
using Emzi0767.CompanionCube.Services;
using Npgsql;

namespace Emzi0767.CompanionCube
{
    /// <summary>
    /// Entry point of the bot's binary.
    /// </summary>
    internal class Program
    {
        private static Dictionary<int, CompanionCubeBot> Shards { get; set; }

        /// <summary>
        /// Wrapper for asynchronous entry point.
        /// </summary>
        /// <param name="args">Command-line arguments for the binary.</param>
        internal static void Main(string[] args)
        {
            // pass the execution to the asynchronous entry point
            var async = new AsyncExecutor();
            async.Execute(MainAsync(args));
        }

        /// <summary>
        /// Asynchronous entry point of the bot's binary.
        /// </summary>
        /// <param name="args">Command-line arguments for the binary.</param>
        /// <returns></returns>
        private static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Loading Companion Cube...");
            Console.Write("[1/4] Loading configuration         ");

            // locate the config file
            var cfgFile = new FileInfo("config.json");

            // load the config file and validate it
            var cfgLoader = new CompanionCubeConfigLoader();
            var cfg = await cfgLoader.LoadConfigurationAsync(cfgFile).ConfigureAwait(false);
            cfgLoader.ValidateConfiguration(cfg);

            Console.Write("\r[2/4] Loading unicode data          ");

            // load unicode data
            using (var utfloader = new UnicodeDataLoader("unicode_data.json.gz"))
                await utfloader.LoadCodepointsAsync().ConfigureAwait(false);

            Console.Write("\r[3/4] Validating PostgreSQL database");

            // create database type mapping
            NpgsqlConnection.GlobalTypeMapper.MapEnum<DatabaseEntityKind>("entity_kind");
            NpgsqlConnection.GlobalTypeMapper.MapEnum<DatabaseTagKind>("tag_kind");

            // create database connection and validate schema
            var dbcsp = new ConnectionStringProvider(cfg.PostgreSQL);
            var db = new DatabaseContext(dbcsp);
            var dbv = db.Metadata.SingleOrDefault(x => x.MetaKey == "schema_version");
            if (dbv == null || dbv.MetaValue != "4")
                throw new InvalidDataException("Database schema version mismatch.");
            dbv = db.Metadata.SingleOrDefault(x => x.MetaKey == "project");
            if (dbv == null || dbv.MetaValue != "Companion Cube")
                throw new InvalidDataException("Database schema type mismatch.");

            Console.Write("\r[4/4] Creating and booting shards   ");

            // create shards
            Shards = new Dictionary<int, CompanionCubeBot>();
            var async = new AsyncExecutor();
            for (int i = 0; i < cfg.Discord.ShardCount; i++)
                Shards[i] = new CompanionCubeBot(cfg, i, async);

            // --- LOADING COMPLETED ---
            Console.WriteLine("\rLoading completed, booting the shards");
            Console.WriteLine("-------------------------------------");

            // boot shards
            foreach (var (k, shard) in Shards)
                await shard.StartAsync();

            // do a minimal cleanup
            GC.Collect();

            // wait forever
            await Task.Delay(-1);
        }
    }
}