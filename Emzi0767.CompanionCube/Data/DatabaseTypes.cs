// This file is a part of Companion Cube project.
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Npgsql;
using NpgsqlTypes;

namespace Emzi0767.CompanionCube.Data
{
    /// <summary>
    /// Represents kind of an entity with associated ID.
    /// </summary>
    public enum DatabaseEntityKind
    {
        /// <summary>
        /// Defines that the entity is a user.
        /// </summary>
        [PgName("user")]
        User,

        /// <summary>
        /// Defines that the entity is a channel.
        /// </summary>
        [PgName("channel")]
        Channel,

        /// <summary>
        /// Defines that the entity is a guild.
        /// </summary>
        [PgName("guild")]
        Guild
    }

    /// <summary>
    /// Represents kind of a tag in the database.
    /// </summary>
    public enum DatabaseTagKind
    {
        /// <summary>
        /// Defines that the tag is bound to a channel.
        /// </summary>
        [PgName("channel")]
        Channel,

        /// <summary>
        /// Defines that the tag is bound to a guild.
        /// </summary>
        [PgName("guild")]
        Guild,

        /// <summary>
        /// Defines that the tag is not bound, and it will appear and be usable everywhere.
        /// </summary>
        [PgName("global")]
        Global
    }
}
