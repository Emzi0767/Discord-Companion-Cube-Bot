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
using Emzi0767.CompanionCube.Data;

namespace Emzi0767.CompanionCube.Services
{
    /// <summary>
    /// A minimal connection string provider service for PostgreSQL connections.
    /// </summary>
    public sealed class ConnectionStringProvider
    {
        private CompanionCubeConfigPostgres Config { get; }
        private Lazy<string> ConnectionString { get; }

        /// <summary>
        /// Creates a new instance of the string provider service.
        /// </summary>
        /// <param name="cfg"></param>
        public ConnectionStringProvider(CompanionCubeConfigPostgres cfg)
        {
            this.Config = cfg;
            this.ConnectionString = new Lazy<string>(() => this.Config.ToPostgresConnectionString());
        }

        /// <summary>
        /// Gets the PostgreSQL connection string.
        /// </summary>
        /// <returns>PostgreSQL connection string.</returns>
        public string GetConnectionString()
            => this.ConnectionString.Value;
    }
}
