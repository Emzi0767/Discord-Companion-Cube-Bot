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
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Emzi0767.CompanionCube.Data
{
    /// <summary>
    /// Loader for JSON configuration files. Provides loading and validation logic.
    /// </summary>
    public sealed class CompanionCubeConfigLoader
    {
        /// <summary>
        /// Gets the version of the bot binary.
        /// </summary>
        public string BotVersion { get; }

        /// <summary>
        /// Gets the expected version of configuration data.
        /// </summary>
        public int ConfigVersion { get; }

        /// <summary>
        /// Creates a new instance of the configuration loader.
        /// </summary>
        public CompanionCubeConfigLoader()
        {
            // extract the bot version from bot's assembly
            this.BotVersion = CompanionCubeUtilities.GetBotVersion();

            // set the expected config version
            this.ConfigVersion = 2;
        }

        /// <summary>
        /// Loads the specified configuration file.
        /// </summary>
        /// <param name="file">File to load configuration data from.</param>
        /// <returns>Loaded configuration.</returns>
        /// <exception cref="ArgumentException">Supplied file is null, invalid, or nonexistent.</exception>
        public async Task<CompanionCubeConfig> LoadConfigurationAsync(FileInfo file)
        {
            // validate the file object is ok
            if (file == null || !file.Exists)
                throw new ArgumentException("Specified file is not valid or does not exist.", nameof(file));

            // load the raw json data
            var json = "{}";
            using (var fs = file.OpenRead())
            using (var sr = new StreamReader(fs, CompanionCubeUtilities.UTF8))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            // deserialize the config
            return JsonConvert.DeserializeObject<CompanionCubeConfig>(json);
        }

        /// <summary>
        /// Saves the specified configuration to specified file. The file will be overwritten.
        /// </summary>
        /// <param name="config">Configuration data to save.</param>
        /// <param name="file">File to save the data to.</param>
        /// <returns></returns>
        public async Task SaveConfigurationAsync(CompanionCubeConfig config, FileInfo file)
        {
            // validate the config first
            this.ValidateConfiguration(config);

            // serialize the config
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);

            // write the config to a file
            using (var fs = file.Create())
            using (var sr = new StreamWriter(fs, CompanionCubeUtilities.UTF8))
            {
                await sr.WriteLineAsync(json).ConfigureAwait(false);
                await sr.FlushAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Validates specified configuration. Will throw if data is invalid.
        /// </summary>
        /// <param name="config">Configuration data to validate</param>
        public void ValidateConfiguration(CompanionCubeConfig config)
        {
            // validate the config
            if (config == null || config.Version == null || config.Discord == null || config.PostgreSQL == null || config.Lavalink == null || config.YouTube == null)
                throw new ArgumentNullException(nameof(config), "Configuration data, or one of its parts, is null.");

            // validate config version
            if (config.Version.Configuration != this.ConfigVersion)
                throw new InvalidDataException("Configuration data version mismatch.");
        }
    }
}
