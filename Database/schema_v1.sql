-- Companion Cube database schema
-- for PostgreSQL 9.6+
-- 
-- Author:          Emzi0767
-- Project:         Companion Cube
-- Version:         1
-- Last update:     2017-07-22 20:20
-- Requires:        fuzzystrmatch
--
-- ------------------------------------------------------------------------
-- 
-- This file is part of Companion Cube project
--
-- Copyright 2018 Emzi0767
-- 
-- Licensed under the Apache License, Version 2.0 (the "License");
-- you may not use this file except in compliance with the License.
-- You may obtain a copy of the License at
-- 
--   http://www.apache.org/licenses/LICENSE-2.0
-- 
-- Unless required by applicable law or agreed to in writing, software
-- distributed under the License is distributed on an "AS IS" BASIS,
-- WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
-- See the License for the specific language governing permissions and
-- limitations under the License.
--
-- ------------------------------------------------------------------------

-- Load the fuzzystrmatch extension. This is required for tags.
CREATE EXTENSION fuzzystrmatch;

-- ------------------------------------------------------------------------

-- cc_database_info
-- This table holds all metadata about the database schema, such as schema 
-- version. This table should not be altered manually.
CREATE SEQUENCE cc_database_info_id_seq;
CREATE TABLE cc_database_info(
    id INTEGER PRIMARY KEY DEFAULT NEXTVAL('cc_database_info_id_seq'),
    config_key TEXT NOT NULL,
    config_value TEXT NOT NULL,
    UNIQUE(config_key)
);
ALTER SEQUENCE cc_database_info_id_seq OWNED BY cc_database_info.id;

-- Insert metadata into the table.
INSERT INTO cc_database_info(config_key, config_value) VALUES('schema_version', '1');

-- ------------------------------------------------------------------------

-- cc_prefixes
-- This table holds information about all prefixes configured for the bot, 
-- on per-channel or per-guild basis. For more information, refer to bot's 
-- source code.
CREATE SEQUENCE cc_prefixes_id_seq;
CREATE TABLE cc_prefixes(
    id BIGINT PRIMARY KEY DEFAULT NEXTVAL('cc_prefixes_id_seq'),
    channel_id BIGINT, -- Snowflake
    guild_id BIGINT, -- Snowflake
    prefix TEXT NOT NULL,
    UNIQUE(channel_id),
    UNIQUE(guild_id)
);
ALTER SEQUENCE cc_prefixes_id_seq OWNED BY cc_prefixes.id;

-- ------------------------------------------------------------------------

-- cc_tags
-- This table holds all the tags created by the users. Tags are created on 
-- per-channel basis, and by default are hidden from search results, unless
-- explicitly approved by a moderator or bot's owner. Additionally, each 
-- tag is stored with full history.
CREATE SEQUENCE cc_tags_id_seq;
CREATE TABLE cc_tags(
    id BIGINT PRIMARY KEY DEFAULT NEXTVAL('cc_tags_id_seq'),
    channel_id BIGINT NOT NULL, -- Snowflake
    owner_id BIGINT NOT NULL, -- Snowflake
    name TEXT NOT NULL,
    contents TEXT[] NOT NULL,
    edits TIMESTAMP WITH TIME ZONE[] NOT NULL,
    editing_user_ids BIGINT[] NOT NULL,
    uses BIGINT NOT NULL DEFAULT 0,
    hidden BOOLEAN NOT NULL DEFAULT TRUE,
    UNIQUE(channel_id, name)
);
ALTER SEQUENCE cc_tags_id_seq OWNED BY cc_tags.id;

-- ------------------------------------------------------------------------

-- cc_currency
-- This table holds information about each user's virtual currency 
-- information. The currency is called Shitpost Coin, and is randomly 
-- issued based on a chance per message. All currency info is cross-guild, 
-- meaning sending messages in one guild will issue currency for all 
-- guilds.

CREATE SEQUENCE cc_currency_id_seq;
CREATE TABLE cc_currency(
    id BIGINT PRIMARY KEY DEFAULT NEXTVAL('cc_currency_id_seq'),
    user_id BIGINT NOT NULL, -- Snowflake
    amount BIGINT NOT NULL DEFAULT 0,
    UNIQUE(user_id)
);
ALTER SEQUENCE cc_currency_id_seq OWNED BY cc_currency.id;

-- ------------------------------------------------------------------------

-- cc_blocked_users
-- This table contains list of users who are blocked from using the bot, 
-- and thus are completely ignored.

CREATE SEQUENCE cc_blocked_users_id_seq;
CREATE TABLE cc_blocked_users(
    id BIGINT PRIMARY KEY DEFAULT NEXTVAL('cc_blocked_users_id_seq'),
    user_id BIGINT NOT NULL, -- Snowflake
    UNIQUE(user_id)
);
ALTER SEQUENCE cc_blocked_users_id_seq OWNED BY cc_blocked_users.id;

-- ------------------------------------------------------------------------

-- cc_blocked_channels
-- This table contains list of channels that will be ignored by the bot.

CREATE SEQUENCE cc_blocked_channels_id_seq;
CREATE TABLE cc_blocked_channels(
    id BIGINT PRIMARY KEY DEFAULT NEXTVAL('cc_blocked_channels_id_seq'),
    channel_id BIGINT NOT NULL, -- Snowflake
    UNIQUE(channel_id)
);
ALTER SEQUENCE cc_blocked_channels_id_seq OWNED BY cc_blocked_channels.id;

-- ------------------------------------------------------------------------

-- cc_blocked_guilds
-- This table contains list of guilds that will be ignored by the bot.

CREATE SEQUENCE cc_blocked_guilds_id_seq;
CREATE TABLE cc_blocked_guilds(
    id BIGINT PRIMARY KEY DEFAULT NEXTVAL('cc_blocked_guilds_id_seq'),
    guild_id BIGINT NOT NULL, -- Snowflake
    UNIQUE(guild_id)
);
ALTER SEQUENCE cc_blocked_guilds_id_seq OWNED BY cc_blocked_guilds.id;