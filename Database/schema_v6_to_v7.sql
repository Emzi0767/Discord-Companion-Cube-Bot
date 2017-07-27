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
-- Copyright (C) 2018-2021 Emzi0767
-- 
-- This program is free software: you can redistribute it and/or modify
-- it under the terms of the GNU Affero General Public License as published by
-- the Free Software Foundation, either version 3 of the License, or
-- (at your option) any later version.
-- 
-- This program is distributed in the hope that it will be useful,
-- but WITHOUT ANY WARRANTY; without even the implied warranty of
-- MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
-- GNU Affero General Public License for more details.
-- 
-- You should have received a copy of the GNU Affero General Public License
-- along with this program.  If not, see <https://www.gnu.org/licenses/>.
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
