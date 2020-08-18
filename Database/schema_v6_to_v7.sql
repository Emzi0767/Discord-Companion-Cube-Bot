-- Companion Cube database schema
-- for PostgreSQL 9.6+
-- 
-- Author:          Emzi0767
-- Project:         Companion Cube
-- Version:         6 to 7
-- Last update:     2020-08-1 01:30 +02:00
--
-- ------------------------------------------------------------------------
-- 
-- This file is part of Companion Cube project
--
-- Copyright 2020 Emzi0767
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
-- Tables, migrations, and conversions

-- Update schema version in the database
update metadata set meta_value = '7' where meta_key = 'schema_version';
update metadata set meta_value = '2020-08-18T01:30+02:00' where meta_key = 'timestamp';

-- ------------------------------------------------------------------------

-- rss_feeds
create table rss_feeds(
	name text not null,
	url text not null,
	channel_id bigint not null,
	last_timestamp timestamptz default null,
	init_replay int default null,
	primary key(url, channel_id),
	unique(name, channel_id)
);

create index ix_rss_url on rss_feeds(url);
create index ix_rss_channel on rss_feeds(channel_id);
create index ix_rss_name on rss_feeds(name);
