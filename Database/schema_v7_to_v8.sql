-- Companion Cube database schema
-- for PostgreSQL 9.6+
-- 
-- Author:          Emzi0767
-- Project:         Companion Cube
-- Version:         7 to 8
-- Last update:     2020-09-10 18:55 +02:00
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
update metadata set meta_value = '8' where meta_key = 'schema_version';
update metadata set meta_value = '2020-09-10T18:55+02:00' where meta_key = 'timestamp';

-- ------------------------------------------------------------------------

-- pooper_whitelist
create table pooper_whitelist(
	guild_id bigint not null,
	comment text default null,
	primary key(guild_id)
);

create index ix_pooper_guild_id on pooper_whitelist(guild_id);
