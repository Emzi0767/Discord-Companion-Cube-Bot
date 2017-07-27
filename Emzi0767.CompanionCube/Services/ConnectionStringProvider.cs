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
