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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Emzi0767.CompanionCube.Exceptions;
using Emzi0767.CompanionCube.Modules;
using Npgsql;
using NpgsqlTypes;

namespace Emzi0767.CompanionCube.Services
{
    public sealed class DatabaseClient
    {
        public const string SCHEMA_VERSION = "1";

        private string ConnectionString { get; }
        private CompanionCubeDatabaseConfig Configuration { get; }
        private SemaphoreSlim Semaphore { get; }
        private SemaphoreSlim TransactionSemaphore { get; }

        public DatabaseClient(CompanionCubeDatabaseConfig config)
        {
            this.Configuration = config;
            this.Semaphore = new SemaphoreSlim(100, 100);
            this.TransactionSemaphore = new SemaphoreSlim(1, 1);

            var csb = new NpgsqlConnectionStringBuilder()
            {
                Host = this.Configuration.Hostname,
                Port = this.Configuration.Port,
                Database = this.Configuration.Database,
                Username = this.Configuration.Username,
                Password = this.Configuration.Password,
                Pooling = true,

                SslMode = SslMode.Require,
                TrustServerCertificate = true
            };
            this.ConnectionString = csb.ConnectionString;
        }

        public async Task InitializeAsync()
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "database_info");
                cmd.CommandText = string.Concat("SELECT config_value FROM ", tbl, " WHERE config_key='schema_version';");

                var res = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                if (res == null || res is DBNull)
                    throw new DatabaseClientException("Database schema is misconfigured. Refer to installation manual for proper instructions.");
                
                if ((string)res != SCHEMA_VERSION)
                    throw new DatabaseClientException(string.Concat("Database schema has incorrect version. Current: ", (string)res, "; Expected: ", SCHEMA_VERSION, "."));
            }

            this.Semaphore.Release();
        }

        public async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> ExecuteRawQueryAsync(string query)
        {
            await this.Semaphore.WaitAsync();
            var dicts = new List<IReadOnlyDictionary<string, string>>();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                cmd.CommandText = query;

                using (var rdr = await cmd.ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        var dict = new Dictionary<string, string>();

                        for (var i = 0; i < rdr.FieldCount; i++)
                            dict[rdr.GetName(i)] = rdr[i] is DBNull ? "<null>" : rdr[i].ToString();

                        dicts.Add(new ReadOnlyDictionary<string, string>(dict));
                    }
                }
            }

            this.Semaphore.Release();
            return new ReadOnlyCollection<IReadOnlyDictionary<string, string>>(dicts);
        }

        public async Task<IReadOnlyDictionary<ulong, string>> GetChannelPrefixesAsync()
        {
            await this.Semaphore.WaitAsync();
            var dict = new Dictionary<ulong, string>();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "prefixes");
                cmd.CommandText = string.Concat("SELECT channel_id, prefix FROM ", tbl, " WHERE guild_id IS NULL;");
                
                using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                        dict[(ulong)(long)reader["channel_id"]] = (string)reader["prefix"];
                }
            }

            this.Semaphore.Release();
            return new ReadOnlyDictionary<ulong, string>(dict);
        }

        public async Task<IReadOnlyDictionary<ulong, string>> GetGuildPrefixesAsync()
        {
            await this.Semaphore.WaitAsync();
            var dict = new Dictionary<ulong, string>();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "prefixes");
                cmd.CommandText = string.Concat("SELECT guild_id, prefix FROM ", tbl, " WHERE channel_id IS NULL;");
                
                using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                        dict[(ulong)(long)reader["guild_id"]] = (string)reader["prefix"];
                }
            }

            this.Semaphore.Release();
            return new ReadOnlyDictionary<ulong, string>(dict);
        }

        public async Task SetChannelPrefixAsync(ulong channel_id, string prefix)
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "prefixes");
                cmd.CommandText = string.Concat("INSERT INTO ", tbl, "(channel_id, guild_id, prefix) VALUES(@channel_id, NULL, @prefix) ON CONFLICT(channel_id) DO UPDATE SET prefix = EXCLUDED.prefix;");

                cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)channel_id);
                cmd.Parameters.AddWithValue("prefix", NpgsqlDbType.Text, prefix);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            this.Semaphore.Release();
        }

        public async Task SetGuildPrefixAsync(ulong guild_id, string prefix)
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "prefixes");
                cmd.CommandText = string.Concat("INSERT INTO ", tbl, "(channel_id, guild_id, prefix) VALUES(NULL, @guild_id, @prefix) ON CONFLICT(guild_id) DO UPDATE SET prefix = EXCLUDED.prefix;");

                cmd.Parameters.AddWithValue("guild_id", NpgsqlDbType.Bigint, (long)guild_id);
                cmd.Parameters.AddWithValue("prefix", NpgsqlDbType.Text, prefix);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            this.Semaphore.Release();
        }

        public async Task ResetChannelPrefixAsync(ulong channel_id)
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "prefixes");
                cmd.CommandText = string.Concat("DELETE FROM ", tbl, " WJERE channel_id = @channel_id;");

                cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)channel_id);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            this.Semaphore.Release();
        }

        public async Task ResetGuildPrefixAsync(ulong guild_id)
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "prefixes");
                cmd.CommandText = string.Concat("DELETE FROM ", tbl, " WJERE guild_id = @guild_id;");

                cmd.Parameters.AddWithValue("guild_id", NpgsqlDbType.Bigint, (long)guild_id);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            this.Semaphore.Release();
        }

        public async Task<IReadOnlyList<ulong>> GetBlockedUsersAsync()
        {
            await this.Semaphore.WaitAsync();
            var list = new List<ulong>();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "blocked_users");
                cmd.CommandText = string.Concat("SELECT user_id FROM ", tbl, ";");
                
                using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                        list.Add((ulong)(long)reader["user_id"]);
                }
            }
            
            this.Semaphore.Release();
            return new ReadOnlyCollection<ulong>(list);
        }

        public async Task<IReadOnlyList<ulong>> GetBlockedChannelsAsync()
        {
            await this.Semaphore.WaitAsync();
            var list = new List<ulong>();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "blocked_channels");
                cmd.CommandText = string.Concat("SELECT channel_id FROM ", tbl, ";");
                
                using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                        list.Add((ulong)(long)reader["channel_id"]);
                }
            }
            
            this.Semaphore.Release();
            return new ReadOnlyCollection<ulong>(list);
        }

        public async Task<IReadOnlyList<ulong>> GetBlockedGuildsAsync()
        {
            await this.Semaphore.WaitAsync();
            var list = new List<ulong>();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "blocked_guilds");
                cmd.CommandText = string.Concat("SELECT guild_id FROM ", tbl, ";");
                
                using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                        list.Add((ulong)(long)reader["guilds_id"]);
                }
            }
            
            this.Semaphore.Release();
            return new ReadOnlyCollection<ulong>(list);
        }

        public async Task BlockUserAsync(ulong user_id)
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "blocked_users");
                cmd.CommandText = string.Concat("INSERT INTO ", tbl, "(user_id) VALUES(@user_id) ON CONFLICT(user_id) DO NOTHING;");

                cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, (long)user_id);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            
            this.Semaphore.Release();
        }

        public async Task UnblockUserAsync(ulong user_id)
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "blocked_users");
                cmd.CommandText = string.Concat("DELETE FROM ", tbl, " WHERE user_id = @user_id;");

                cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, (long)user_id);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            
            this.Semaphore.Release();
        }

        public async Task BlockChannelAsync(ulong channel_id)
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "blocked_channels");
                cmd.CommandText = string.Concat("INSERT INTO ", tbl, "(channel_id) VALUES(@channel_id) ON CONFLICT(channel_id) DO NOTHING;");

                cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)channel_id);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            
            this.Semaphore.Release();
        }

        public async Task UnblockChannelAsync(ulong channel_id)
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "blocked_channels");
                cmd.CommandText = string.Concat("DELETE FROM ", tbl, " WHERE channel_id = @channel_id;");

                cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)channel_id);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            
            this.Semaphore.Release();
        }

        public async Task BlockGuildAsync(ulong guild_id)
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "blocked_guilds");
                cmd.CommandText = string.Concat("INSERT INTO ", tbl, "(guild_id) VALUES(@guild_id) ON CONFLICT(guild_id) DO NOTHING;");

                cmd.Parameters.AddWithValue("guild_id", NpgsqlDbType.Bigint, (long)guild_id);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            
            this.Semaphore.Release();
        }

        public async Task UnblockGuildAsync(ulong guild_id)
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "blocked_guilds");
                cmd.CommandText = string.Concat("DELETE FROM ", tbl, " WHERE guild_id = @guild_id;");

                cmd.Parameters.AddWithValue("guild_id", NpgsqlDbType.Bigint, (long)guild_id);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            
            this.Semaphore.Release();
        }

        public async Task IssueCurrencyAsync(ulong user, long amount)
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "currency");
                cmd.CommandText = string.Concat("INSERT INTO ", tbl, "(user_id, amount) VALUES(@user_id, @amount) ON CONFLICT(user_id) DO UPDATE SET amount = ", tbl, ".amount + EXCLUDED.amount;");

                cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, (long)user);
                cmd.Parameters.AddWithValue("amount", NpgsqlDbType.Bigint, amount);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            this.Semaphore.Release();
        }

        public async Task<long> GetCurrencyAsync(ulong user)
        {
            await this.Semaphore.WaitAsync();
            var shekels = 0L;

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "currency");
                cmd.CommandText = string.Concat("SELECT amount FROM ", tbl, " WHERE user_id = @user_id LIMIT 1;");

                cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, (long)user);

                var res = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                if (res != null && !(res is DBNull))
                    shekels = (long)res;
            }

            this.Semaphore.Release();
            return shekels;
        }

        public async Task TransferCurrencyAsync(ulong source, ulong target, long amount)
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            {
                await con.OpenAsync().ConfigureAwait(false);
                var tbl = string.Concat(this.Configuration.TableNamePrefix, "currency");

                // check if target exists
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = string.Concat("SELECT amount FROM ", tbl, " WHERE user_id = @user_id;");

                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, (long)target);

                    var res = await cmd.ExecuteScalarAsync().ConfigureAwait(false);

                    // nope, fix that
                    if (res == null || res is DBNull)
                        await this.IssueCurrencyAsync(target, 0);
                }

                await this.TransactionSemaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    using (var transaction = con.BeginTransaction())
                    {
                        // lock
                        var cmd1 = con.CreateCommand();
                        cmd1.Transaction = transaction;
                        cmd1.CommandText = string.Concat("SELECT * FROM ", tbl, " WHERE user_id = @user_id1 OR user_id = @user_id2 FOR UPDATE;");

                        cmd1.Parameters.AddWithValue("user_id1", NpgsqlDbType.Bigint, (long)source);
                        cmd1.Parameters.AddWithValue("user_id2", NpgsqlDbType.Bigint, (long)target);

                        await cmd1.ExecuteNonQueryAsync().ConfigureAwait(false);

                        // check source
                        var cmd2 = con.CreateCommand();
                        cmd2.Transaction = transaction;
                        cmd2.CommandText = string.Concat("SELECT amount FROM ", tbl, " WHERE user_id = @user_id;");

                        cmd2.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, (long)source);

                        var res = await cmd2.ExecuteScalarAsync().ConfigureAwait(false);
                        if (res == null || res is DBNull || (long)res < amount)
                        {
                            await transaction.RollbackAsync().ConfigureAwait(false);
                            throw new CurrencyException("Source user's currency amount is insufficient for the transfer.");
                        }

                        // subtract
                        var cmd3 = con.CreateCommand();
                        cmd3.Transaction = transaction;
                        cmd3.CommandText = string.Concat("UPDATE ", tbl, " SET amount = amount - @amount WHERE user_id = @user_id;");

                        cmd3.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, (long)source);
                        cmd3.Parameters.AddWithValue("amount", NpgsqlDbType.Bigint, amount);

                        await cmd3.ExecuteNonQueryAsync().ConfigureAwait(false);

                        // add
                        var cmd4 = con.CreateCommand();
                        cmd4.Transaction = transaction;
                        cmd4.CommandText = string.Concat("UPDATE ", tbl, " SET amount = amount + @amount WHERE user_id = @user_id;");

                        cmd4.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, (long)target);
                        cmd4.Parameters.AddWithValue("amount", NpgsqlDbType.Bigint, amount);

                        await cmd4.ExecuteNonQueryAsync().ConfigureAwait(false);

                        // all good, go
                        await transaction.CommitAsync().ConfigureAwait(false);

                        cmd1.Dispose();
                        cmd2.Dispose();
                        cmd3.Dispose();
                        cmd4.Dispose();
                    }
                }
                finally
                {
                    this.TransactionSemaphore.Release();
                    this.Semaphore.Release();
                }
            }
        }

        public async Task<bool> CreateTagAsync(ulong author_id, ulong channel_id, string name, string contents)
        {
            await this.Semaphore.WaitAsync();
            var success = false;

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "tags");
                cmd.CommandText = string.Concat("INSERT INTO ", tbl, "(channel_id, owner_id, name, contents, edits, editing_user_ids) VALUES(@channel_id, @owner_id, @name, @contents, @edits, @editing_user_ids) ON CONFLICT(channel_id, name) DO NOTHING;");

                cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)channel_id);
                cmd.Parameters.AddWithValue("owner_id", NpgsqlDbType.Bigint, (long)author_id);
                cmd.Parameters.AddWithValue("name", NpgsqlDbType.Text, name);
                cmd.Parameters.AddWithValue("contents", NpgsqlDbType.Text | NpgsqlDbType.Array, new[] { contents });
                cmd.Parameters.AddWithValue("edits", NpgsqlDbType.TimestampTZ | NpgsqlDbType.Array, new[] { DateTimeOffset.Now });
                cmd.Parameters.AddWithValue("editing_user_ids", NpgsqlDbType.Bigint | NpgsqlDbType.Array, new[] { (long)author_id });

                var affected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                if (affected > 0)
                    success = true;
            }

            this.Semaphore.Release();
            return success;
        }

        public async Task<TagResult> GetTagAsync(ulong channel_id, string name)
        {
            await this.Semaphore.WaitAsync();
            var result = new TagResult { IsSuccess = false };

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "tags");
                cmd.CommandText = string.Concat("SELECT id, channel_id, owner_id, name, contents, edits, editing_user_ids, uses, hidden FROM ", tbl, " WHERE channel_id = @channel_id AND levenshtein(name, @name) < 3 LIMIT 5;");

                cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)channel_id);
                cmd.Parameters.AddWithValue("name", NpgsqlDbType.Text, name);

                using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    var tags = new List<Tag>();
                    var the_only = default(Tag);
                    var found = false;
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var tag = new Tag
                        {
                            Id = (long)reader["id"],
                            Name = (string)reader["name"],
                            ChannelId = (ulong)(long)reader["channel_id"],
                            OwnerId = (ulong)(long)reader["owner_id"],
                            Contents = new ReadOnlyCollection<string>((string[])reader["contents"]),
                            Edits = new ReadOnlyCollection<DateTimeOffset>(((DateTime[])reader["edits"]).Select(xdt => new DateTimeOffset(xdt)).ToArray()),
                            EditingUserIds = new ReadOnlyCollection<ulong>(((long[])reader["editing_user_ids"]).Select(xl => (ulong)xl).ToArray()),
                            IsHidden = (bool)reader["hidden"],
                            Uses = (long)reader["uses"]
                        };

                        if (tag.Name == name)
                        {
                            found = true;
                            the_only = tag;
                        }

                        tags.Add(tag);
                    }
                    result = new TagResult
                    {
                        IsSuccess = found,
                        ResultTag = the_only,
                        SuggestedTags = new ReadOnlyCollection<Tag>(tags)
                    };
                }
            }

            this.Semaphore.Release();
            return result;
        }

        public async Task IncrementTagUsageAsync(long tag_id)
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "tags");
                cmd.CommandText = string.Concat("UPDATE ", tbl, " SET uses = uses + 1 WHERE id = @id;");

                cmd.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, tag_id);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            this.Semaphore.Release();
        }

        public async Task<TagResult> ListTagsAsync(ulong channel_id, string name)
        {
            await this.Semaphore.WaitAsync();
            var result = new TagResult { };

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "tags");

                if (name != null)
                {
                    cmd.CommandText = string.Concat("SELECT id, channel_id, owner_id, name, contents, edits, editing_user_ids, uses, hidden FROM ", tbl, " WHERE channel_id = @channel_id AND levenshtein(name, @name) < 3 LIMIT 15;");

                    cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)channel_id);
                    cmd.Parameters.AddWithValue("name", NpgsqlDbType.Text, name);
                }
                else
                {
                    cmd.CommandText = string.Concat("SELECT id, channel_id, owner_id, name, contents, edits, editing_user_ids, uses, hidden FROM ", tbl, " WHERE channel_id = @channel_id AND hidden IS NOT TRUE;");

                    cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)channel_id);
                }

                using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    var tags = new List<Tag>();
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var tag = new Tag
                        {
                            Id = (long)reader["id"],
                            Name = (string)reader["name"],
                            ChannelId = (ulong)(long)reader["channel_id"],
                            OwnerId = (ulong)(long)reader["owner_id"],
                            Contents = new ReadOnlyCollection<string>((string[])reader["contents"]),
                            Edits = new ReadOnlyCollection<DateTimeOffset>(((DateTime[])reader["edits"]).Select(xdt => new DateTimeOffset(xdt)).ToArray()),
                            EditingUserIds = new ReadOnlyCollection<ulong>(((long[])reader["editing_user_ids"]).Select(xl => (ulong)xl).ToArray()),
                            IsHidden = (bool)reader["hidden"],
                            Uses = (long)reader["uses"]
                        };

                        tags.Add(tag);
                    }
                    result = new TagResult
                    {
                        SuggestedTags = new ReadOnlyCollection<Tag>(tags)
                    };
                }
            }

            this.Semaphore.Release();
            return result;
        }

        public async Task SetTagHiddenFlagAsync(ulong channel_id, string name, bool flag)
        {
            await this.Semaphore.WaitAsync();

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);

                var tbl = string.Concat(this.Configuration.TableNamePrefix, "tags");
                cmd.CommandText = string.Concat("UPDATE ", tbl, " SET hidden = @hidden WHERE channel_id = @channel_id AND name = @name;");

                cmd.Parameters.AddWithValue("channel_id", NpgsqlDbType.Bigint, (long)channel_id);
                cmd.Parameters.AddWithValue("name", NpgsqlDbType.Text, name);
                cmd.Parameters.AddWithValue("hidden", NpgsqlDbType.Boolean, flag);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            this.Semaphore.Release();
        }

        public async Task<bool> DeleteTagAsync(long id, ulong user_id, bool force)
        {
            await this.Semaphore.WaitAsync();
            var success = false;

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);
                var tbl = string.Concat(this.Configuration.TableNamePrefix, "tags");

                if (force)
                {
                    cmd.CommandText = string.Concat("DELETE FROM ", tbl, " WHERE id = @id;");
                }
                else
                {
                    cmd.CommandText = string.Concat("DELETE FROM ", tbl, " WHERE id = @id AND owner_id = @owner_id;");
                    cmd.Parameters.AddWithValue("owner_id", NpgsqlDbType.Bigint, user_id);
                }

                cmd.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, id);

                var res = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                success = res > 0;
            }

            this.Semaphore.Release();
            return success;
        }

        public async Task<bool> EditTagAsync(long id, ulong user_id, string contents, bool force)
        {
            await this.Semaphore.WaitAsync();
            var success = false;

            using (var con = new NpgsqlConnection(this.ConnectionString))
            using (var cmd = con.CreateCommand())
            {
                await con.OpenAsync().ConfigureAwait(false);
                var tbl = string.Concat(this.Configuration.TableNamePrefix, "tags");

                if (force)
                {
                    cmd.CommandText = string.Concat("UPDATE ", tbl, " SET contents = array_append(contents, @contents), edits = array_append(edits, @edit), editing_user_ids = array_append(editing_user_ids, @editing_user) WHERE id = @id;");
                }
                else
                {
                    cmd.CommandText = string.Concat("UPDATE ", tbl, " SET contents = array_append(contents, @contents), edits = array_append(edits, @edit), editing_user_ids = array_append(editing_user_ids, @editing_user) WHERE id = @id AND owner_id = @editing_user;");
                }

                cmd.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, id);
                cmd.Parameters.AddWithValue("contents", NpgsqlDbType.Text, contents);
                cmd.Parameters.AddWithValue("edit", NpgsqlDbType.TimestampTZ, DateTimeOffset.Now);
                cmd.Parameters.AddWithValue("editing_user", NpgsqlDbType.Bigint, (long)user_id);

                var res = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                success = res > 0;
            }

            this.Semaphore.Release();
            return success;
        }
    }
}