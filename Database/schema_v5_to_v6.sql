-- Companion Cube database schema
-- for PostgreSQL 9.6+
-- 
-- Author:          Emzi0767
-- Project:         Companion Cube
-- Version:         5 to 6
-- Last update:     2020-01-10 22:46 +01:00
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
update metadata set meta_value = '6' where meta_key = 'schema_version';
update metadata set meta_value = '2020-01-10T22:46+01:00' where meta_key = 'timestamp';

-- ------------------------------------------------------------------------

-- Add tag marker to tags table
alter table tags add column is_alias boolean default 0;
