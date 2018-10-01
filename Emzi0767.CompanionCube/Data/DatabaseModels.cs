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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emzi0767.CompanionCube.Data
{
    /// <summary>
    /// Represents a table metadata property.
    /// </summary>
    [Table("metadata")]
    public partial class DatabaseMetadata
    {
        /// <summary>
        /// Gets or sets the name of the metadata property.
        /// </summary>
        [Key]
        [Column("meta_key")]
        public string MetaKey { get; set; }

        /// <summary>
        /// Gets or sets the value of the metadata property.
        /// </summary>
        [Required]
        [Column("meta_value")]
        public string MetaValue { get; set; }
    }

    /// <summary>
    /// Represents command prefix configuration for various guilds.
    /// </summary>
    [Table("prefixes")]
    public partial class DatabasePrefix
    {
        /// <summary>
        /// Gets or sets the guild ID for these prefixes.
        /// </summary>
        [Key]
        [Column("guild_id")]
        public long GuildId { get; set; }

        /// <summary>
        /// Gets or sets the prefixes in use for this guild.
        /// </summary>
        [Required]
        [Column("prefixes")]
        public string[] Prefixes { get; set; }

        /// <summary>
        /// Gets or sets whether the default prefixes should remain active in the guild.
        /// </summary>
        [Required]
        [Column("enable_default")]
        public bool? EnableDefault { get; set; }
    }

    /// <summary>
    /// Represents an entity blacklisted from using the bot.
    /// </summary>
    [Table("entity_blacklist")]
    public partial class DatabaseBlacklistedEntity
    {
        /// <summary>
        /// Gets or sets the entity's ID.
        /// </summary>
        [Column("id")]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the entity's kind.
        /// </summary>
        [Column("kind")]
        public DatabaseEntityKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the reason why the entity was blacklisted.
        /// </summary>
        [Column("reason")]
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets when the entity was blacklisted.
        /// </summary>
        [Column("since", TypeName = "timestamp with time zone")]
        public DateTime Since { get; set; }
    }

    /// <summary>
    /// Represents information about a guild whitelisted to use the music module.
    /// </summary>
    [Table("music_whitelist")]
    public partial class DatabaseMusicWhitelistedGuild
    {
        /// <summary>
        /// Gets or sets the ID of the guild whitelisted for music module usage.
        /// </summary>
        [Key]
        [Column("guild_id")]
        public long GuildId { get; set; }

        /// <summary>
        /// Gets or sets the reason why this guild is whitelisted.
        /// </summary>
        [Column("reason")]
        public string Reason { get; set; }
    }

    /// <summary>
    /// Represents a user-created tag.
    /// </summary>
    [Table("tags")]
    public partial class DatabaseTag
    {
        public DatabaseTag()
        {
            this.Revisions = new HashSet<DatabaseTagRevision>();
        }

        /// <summary>
        /// Gets or sets the kind of this tag.
        /// </summary>
        [Column("kind")]
        public DatabaseTagKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the id of this container to which this tag is bound.
        /// </summary>
        [Column("container_id")]
        public long ContainerId { get; set; }

        /// <summary>
        /// Gets or sets the name of this tag.
        /// </summary>
        [Column("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the id of this tag's owner.
        /// </summary>
        [Column("owner_id")]
        public long OwnerId { get; set; }

        /// <summary>
        /// Gets or sets whether this tag is hidden.
        /// </summary>
        [Required]
        [Column("hidden")]
        public bool IsHidden { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the latest revision of the tag.
        /// </summary>
        [Column("latest_revision", TypeName = "timestamp with time zone")]
        public DateTime LatestRevision { get; set; }

        /// <summary>
        /// Gets or sets the revisions for this tag.
        /// </summary>
        [InverseProperty("Tag")]
        public ICollection<DatabaseTagRevision> Revisions { get; set; }
    }

    /// <summary>
    /// Represents a revision of a user-created tag.
    /// </summary>
    [Table("tag_revisions")]
    public partial class DatabaseTagRevision
    {
        /// <summary>
        /// Gets or sets the kind of related tag.
        /// </summary>
        [Column("kind")]
        public DatabaseTagKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the id of this container to which related tag is bound.
        /// </summary>
        [Column("container_id")]
        public long ContainerId { get; set; }

        /// <summary>
        /// Gets or sets the name of related tag.
        /// </summary>
        [Column("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the contents of this revision.
        /// </summary>
        [Required]
        [Column("contents")]
        public string Contents { get; set; }

        /// <summary>
        /// Gets or sets the creation time of this revision.
        /// </summary>
        [Column("created_at", TypeName = "timestamp with time zone")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the id of this revision's author.
        /// </summary>
        [Column("user_id")]
        public long UserId { get; set; }

        /// <summary>
        /// Gets or sets the tag associated with this revision.
        /// </summary>
        [ForeignKey("Kind,ContainerId,Name")]
        [InverseProperty("Revisions")]
        public DatabaseTag Tag { get; set; }
    }
}
