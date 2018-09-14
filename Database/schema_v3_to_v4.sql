-- Companion Cube database schema
-- for PostgreSQL 9.6+
-- 
-- Author:          Emzi0767
-- Project:         Companion Cube
-- Version:         3 to 4
-- Last update:     2018-09-12 22:54 +02:00
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
-- 
-- Types

-- entity_type
-- Determines entity type of the attached ID.
create type entity_kind as enum('user', 'channel', 'guild');

-- tag_kind
-- Determines the kind of tag, whether it's a channel-specific tag, a 
-- guild-specific tag, or a global tag.
create type tag_kind as enum('channel', 'guild', 'global');

-- ------------------------------------------------------------------------
-- 
-- Tables, migrations, and conversions

-- delete obsolete tables
drop table cc_currency;
drop table cc_shekelrates;

-- ------------------------------------------------------------------------

-- cc_database_info -> metadata

-- drop the old table
drop table cc_database_info;

-- create the new table
create table metadata(
    meta_key text not null,
    meta_value text not null,
    primary key(meta_key)
);

-- populate it with data
insert into metadata values
    ('schema_version', '4'),
    ('timestamp', '2018-09-12T22:54+02:00'),
    ('author', 'Emzi0767'),
    ('project', 'Companion Cube'),
    ('license', 'Apache License 2.0');

-- ------------------------------------------------------------------------

-- cc_prefixes -> prefixes

-- rename table to conform to new naming scheme
alter table cc_prefixes rename to prefixes;

-- drop all channel prefixes - they will now be per-guild only
delete from prefixes where guild_id is null;

-- drop the obsolete id and channel id columns
alter table prefixes drop column id;
alter table prefixes drop column channel_id;

-- convert the prefix value to array and rename it to prefixes
alter table prefixes alter column prefix type text[] using array[prefix];
alter table prefixes rename column prefix to prefixes;

-- create primary key on guild id
alter table prefixes add primary key(guild_id);

-- add a new column which specifies whether default prefixes are to 
-- function or not
alter table prefixes add column enable_default boolean not null default true;

-- ------------------------------------------------------------------------

-- cc_blocked_users, cc_blocked_channels, cc_blocked_guilds -> 
--     blocked_entities

-- create a table with new schema
create table blocked_entities(
    id bigint not null, -- snowflake
    kind entity_kind not null,
    reason text,
    since timestamp with time zone not null,
    primary key(id, kind)
);

-- insert data from old tables into the new one, consolidating all of it
insert into blocked_entities select cc_blocked_users.user_id as id, 'user' kind, null reason, timestamp with time zone '2018-09-12T22:54+02:00' since from cc_blocked_users;
insert into blocked_entities select cc_blocked_channels.channel_id as id, 'channel' kind, null reason, timestamp with time zone '2018-09-12T22:54+02:00' since from cc_blocked_channels;
insert into blocked_entities select cc_blocked_guilds.guild_id as id, 'guild' kind, null reason, timestamp with time zone '2018-09-12T22:54+02:00' since from cc_blocked_guilds;

-- drop the old useless tables
drop table cc_blocked_users;
drop table cc_blocked_channels;
drop table cc_blocked_guilds;

-- ------------------------------------------------------------------------

-- cc_musicenabled -> musicenabled

-- rename table to conform to new naming scheme
alter table cc_musicenabled rename to musicenabled;

-- add a column for reason
alter table musicenabled add column reason text;

-- ------------------------------------------------------------------------

-- cc_tags -> tags, tag_revisions

-- create tables with new schema
create table tags(
    kind tag_kind not null,
    container_id bigint not null,
    name text not null,
    owner_id bigint not null,
    hidden boolean not null default false,
    latest_revision timestamp with time zone not null,
    primary key(kind, container_id, name)
);
create table tag_revisions(
    kind tag_kind not null,
    container_id bigint not null,
    name text not null,
    contents text not null,
    created_at timestamp with time zone not null,
    user_id bigint not null,
    primary key(name, created_at),
    foreign key(kind, container_id, name) references tags(kind, container_id, name)
);

-- populate the tag array
insert into tags select 'channel' kind, cc_tags.channel_id as container_id, cc_tags.name as name,
    cc_tags.owner_id as owner_id, cc_tags.hidden as hidden, edits[array_upper(edits, 1)]
    from cc_tags;

-- populate the tag revision array
insert into tag_revisions select 'channel' kind, cc_tags.channel_id as container_id, cc_tags.name as name, 
    unnest(contents), unnest(edits), unnest(editing_user_ids) from cc_tags;

-- drop the old table
drop table cc_tags;

-- create a temporary table to hold information about tag revisions
--create temporary table t_tags(
--    id bigint not null,
--    contents text not null,
--    edit timestamp with time zone not null,
--    editing_user_id bigint not null
--);
--
-- copy all tag revisions into the temporary table
--insert into t_tags select id, unnest(contents), unnest(edits), unnest(editing_user_ids) from cc_tags;
--
-- construct tag data and populate the tag table with the results
--insert into tags select 'channel' kind, cc_tags.channel_id as container_id, cc_tags.name as name, 
--    cc_tags.owner_id as owner_id, cc_tags.hidden as hidden, array(select 
--        row(contents, edit, editing_user_id)::tag_revision from t_tags where t_tags.id = cc_tags.id 
--        order by edit asc) 
--    from cc_tags;
--
-- drop the old and temporary tables
--drop table cc_tags;
--drop table t_tags;
