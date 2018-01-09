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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Emzi0767.CompanionCube.Services;
using Newtonsoft.Json;

namespace Emzi0767.CompanionCube
{
    internal class Program
    {
        private List<CompanionCubeCore> Shards { get; set; }
        private DatabaseClient Database { get; set; }
        private SharedData Shared { get; set; }

        internal static void Main(string[] args)
        {
            var exec = new AsyncExecutor();
            var prog = new Program();
            exec.Execute(prog.MainAsync(args));
        }

        private async Task MainAsync(string[] args)
        {
            Console.WriteLine("Loading Companion Cube...");
            Console.Write("[1/5] Loading configuration        ");

            var json = "{}";
            var utf8 = new UTF8Encoding(false);
            var fi = new FileInfo("config.json");
            if (!fi.Exists)
            {
                Console.WriteLine("\rLoading configuration failed");

                json = JsonConvert.SerializeObject(CompanionCubeConfig.Default, Formatting.Indented);
                using (var fs = fi.Create())
                using (var sw = new StreamWriter(fs, utf8))
                {
                    await sw.WriteAsync(json);
                    await sw.FlushAsync();
                }

                Console.WriteLine("New default configuration file has been written to the following location:");
                Console.WriteLine(fi.FullName);
                Console.WriteLine("Fill it with appropriate values then re-run this program");

                return;
            }

            using (var fs = fi.OpenRead())
            using (var sr = new StreamReader(fs, utf8))
                json = await sr.ReadToEndAsync();
            var cfg = JsonConvert.DeserializeObject<CompanionCubeConfig>(json);

            Console.Write("\r[2/5] Loading unicode data         ");

            using (var utfloader = new UnicodeDataLoader("unicode_data.json.gz"))
                await utfloader.LoadCodepointsAsync().ConfigureAwait(false);

            Console.Write("\r[3/5] Booting PostgreSQL connection");

            Database = new DatabaseClient(cfg.DatabaseConfig);
            await Database.InitializeAsync();

            Console.Write("\r[4/5] Loading data from database   ");

            var cpfixes_db = await Database.GetChannelPrefixesAsync();
            var cpfixes = new ConcurrentDictionary<ulong, string>();
            foreach (var cpfix in cpfixes_db)
                cpfixes.TryAdd(cpfix.Key, cpfix.Value);

            var gpfixes_db = await Database.GetGuildPrefixesAsync();
            var gpfixes = new ConcurrentDictionary<ulong, string>();
            foreach (var gpfix in gpfixes_db)
                gpfixes.TryAdd(gpfix.Key, gpfix.Value);

            var busers_db = await Database.GetBlockedUsersAsync();
            var busers = new ConcurrentHashSet<ulong>();
            foreach (var buser in busers_db)
                busers.TryAdd(buser);

            var bchans_db = await Database.GetBlockedChannelsAsync();
            var bchans = new ConcurrentHashSet<ulong>();
            foreach (var bchan in bchans_db)
                bchans.TryAdd(bchan);

            var bguilds_db = await Database.GetBlockedGuildsAsync();
            var bguilds = new ConcurrentHashSet<ulong>();
            foreach (var bguild in bguilds_db)
                bguilds.TryAdd(bguild);

            var proc = Process.GetCurrentProcess();

            Shared = new SharedData(cpfixes, gpfixes, busers, bchans, bguilds, cfg.CurrencySymbol, proc.StartTime, cfg.Game);

            Console.Write("\r[5/5] Creating shards              ");

            Shards = new List<CompanionCubeCore>();
            for (var i = 0; i < cfg.ShardCount; i++)
            {
                var shard = new CompanionCubeCore(cfg, i, Database, Shared);
                shard.Initialize();
                Shards.Add(shard);
            }

            // --- LOADING COMPLETED ---
            Console.WriteLine("\rLoading completed, booting the shards");
            Console.WriteLine("-------------------------------------");

            foreach (var shard in Shards)
                await shard.StartAsync();

            // do a minimal cleanup
            GC.Collect();

            // wait forever
            await Task.Delay(-1);
        }
    }
}