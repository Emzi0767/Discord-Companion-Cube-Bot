// This file is a part of Companion Cube project.
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

using Emzi0767.CompanionCube.Data;
using Microsoft.EntityFrameworkCore;

namespace Emzi0767.CompanionCube.Services
{
    /// <summary>
    /// Connection context service for Companion Cube's database.
    /// </summary>
    public partial class DatabaseContext : DbContext
    {
        /// <summary>
        /// Gets or sets metadata for this database.
        /// </summary>
        public virtual DbSet<DatabaseMetadata> Metadata { get; set; }

        /// <summary>
        /// Gets or sets configured per-guild prefixes.
        /// </summary>
        public virtual DbSet<DatabasePrefix> Prefixes { get; set; }

        /// <summary>
        /// Gets or sets entities that are blacklisted from using the bot.
        /// </summary>
        public virtual DbSet<DatabaseBlacklistedEntity> EntityBlacklist { get; set; }

        /// <summary>
        /// Gets or sets guilds which are whitelisted to use the music module.
        /// </summary>
        public virtual DbSet<DatabaseMusicWhitelistedGuild> MusicWhitelist { get; set; }

        /// <summary>
        /// Gets or sets defined tags.
        /// </summary>
        public virtual DbSet<DatabaseTag> Tags { get; set; }

        /// <summary>
        /// Gets or sets defined tag revisions.
        /// </summary>
        public virtual DbSet<DatabaseTagRevision> TagRevisions { get; set; }

        private ConnectionStringProvider ConnectionStringProvider { get; }

        /// <summary>
        /// Creates a new database context with specified connection string provider.
        /// </summary>
        /// <param name="csp">Connection string provider to use when connecting to PostgreSQL.</param>
        public DatabaseContext(ConnectionStringProvider csp)
        {
            this.ConnectionStringProvider = csp;
        }

        /// <summary>
        /// Creates a new database context with specified context options and connection string provider.
        /// </summary>
        /// <param name="options">Database context options.</param>
        /// <param name="csp">Connection string provider to use when connecting to PostgreSQL.</param>
        public DatabaseContext(DbContextOptions<DatabaseContext> options, ConnectionStringProvider csp)
            : base(options)
        {
            this.ConnectionStringProvider = csp;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseNpgsql(this.ConnectionStringProvider.GetConnectionString());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ForNpgsqlHasEnum(null, "entity_kind", new[] { "user", "channel", "guild" })
                .ForNpgsqlHasEnum(null, "tag_kind", new[] { "channel", "guild", "global" })
                .HasPostgresExtension("fuzzystrmatch");

            modelBuilder.Entity<DatabaseMetadata>(entity =>
            {
                entity.Property(e => e.MetaKey).ValueGeneratedNever();
            });

            modelBuilder.Entity<DatabasePrefix>(entity =>
            {
                entity.HasIndex(e => e.GuildId)
                    .HasName("cc_prefixes_guild_id_key")
                    .IsUnique();

                entity.Property(e => e.GuildId).ValueGeneratedNever();

                entity.Property(e => e.EnableDefault).HasDefaultValueSql("true");
            });

            modelBuilder.Entity<DatabaseBlacklistedEntity>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.Kind });
            });

            modelBuilder.Entity<DatabaseMusicWhitelistedGuild>(entity =>
            {
                entity.Property(e => e.GuildId).ValueGeneratedNever();
            });

            modelBuilder.Entity<DatabaseTag>(entity =>
            {
                entity.HasKey(e => new { e.Kind, e.ContainerId, e.Name });
            });

            modelBuilder.Entity<DatabaseTagRevision>(entity =>
            {
                entity.HasKey(e => new { e.Name, e.CreatedAt });

                entity.HasOne(d => d.Tag)
                    .WithMany(p => p.Revisions)
                    .HasForeignKey(d => new { d.Kind, d.ContainerId, d.Name })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("tag_revisions_container_id_fkey");
            });
        }
    }
}
