-- Companion Cube database schema
-- for PostgreSQL 10+
-- 
-- Author:          Emzi0767
-- Project:         Companion Cube
-- Version:         8 to 9
-- Last update:     2021-02-26 09:57 +01:00
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
update metadata set meta_value = '9' where meta_key = 'schema_version';
update metadata set meta_value = '2021-02-26T09:57+01:00' where meta_key = 'timestamp';

-- ------------------------------------------------------------------------

-- create trigram index on tags
create index ix_tags_name_trgm on tags using gin(name gin_trgm_ops);
