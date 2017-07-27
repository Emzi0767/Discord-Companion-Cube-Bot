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

        /// <summary>
        /// Gets or sets defiend RSS feeds.
        /// </summary>
        public virtual DbSet<DatabaseRssFeed> RssFeeds { get; set; }

        /// <summary>
        /// Gets or sets the pooper whitelist entries.
        /// </summary>
        public virtual DbSet<DatabasePooperWhitelist> PooperWhitelist { get; set; }

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
                optionsBuilder.UseNpgsql(this.ConnectionStringProvider.GetConnectionString(), opts => opts.UseTrigrams());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresEnum<DatabaseEntityKind>()
                .HasPostgresEnum<DatabaseTagKind>()
                .HasPostgresExtension("pg_trgm");

            modelBuilder.Entity<DatabaseMetadata>(entity =>
                entity.Property(e => e.MetaKey).ValueGeneratedNever());

            modelBuilder.Entity<DatabasePrefix>(entity =>
            {
                entity.HasIndex(e => e.GuildId)
                    .HasDatabaseName("cc_prefixes_guild_id_key")
                    .IsUnique();

                entity.Property(e => e.GuildId).ValueGeneratedNever();

                entity.Property(e => e.EnableDefault).HasDefaultValueSql("true");
            });

            modelBuilder.Entity<DatabaseBlacklistedEntity>(entity =>
                entity.HasKey(e => new { e.Id, e.Kind }));

            modelBuilder.Entity<DatabaseMusicWhitelistedGuild>(entity =>
                entity.Property(e => e.GuildId).ValueGeneratedNever());

            modelBuilder.Entity<DatabaseTag>(entity =>
            {
                entity.HasKey(e => new { e.Kind, e.ContainerId, e.Name });

                entity.HasIndex(e => e.Name)
                    .HasMethod("gin")
                    .HasOperators("gin_trgm_ops");
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

            modelBuilder.Entity<DatabaseRssFeed>(entity =>
            {
                entity.HasKey(e => new { e.Url, e.ChannelId });

                entity.HasAlternateKey(e => new { e.Name, e.ChannelId });

                entity.HasIndex(e => e.Url)
                    .HasDatabaseName("ix_rss_url");

                entity.HasIndex(e => e.ChannelId)
                    .HasDatabaseName("ix_rss_channel");

                entity.HasIndex(e => e.Name)
                    .HasDatabaseName("ix_rss_name");
            });

            modelBuilder.Entity<DatabasePooperWhitelist>(entity
                => entity.HasIndex(e => e.GuildId)
                    .HasDatabaseName("ix_pooper_guild_id"));
        }
    }
}
