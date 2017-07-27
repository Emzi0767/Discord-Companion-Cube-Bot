-- Companion Cube database schema
-- for PostgreSQL 9.6+
-- 
-- Author:          Emzi0767
-- Project:         Companion Cube
-- Version:         4 to 5
-- Last update:     2018-10-01 19:46 +02:00
-- Requires:        fuzzystrmatch
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
update metadata set meta_value = '5' where meta_key = 'schema_version';
update metadata set meta_value = '2018-10-01T19:46+02:00' where meta_key = 'timestamp';

-- ------------------------------------------------------------------------

-- blocked_entities -> entity_blacklist
alter table blocked_entities rename to entity_blacklist;

-- ------------------------------------------------------------------------

-- musicenabled -> music_whitelist
alter table musicenabled rename to music_whitelist;
