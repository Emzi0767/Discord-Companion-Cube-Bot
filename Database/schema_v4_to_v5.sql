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
