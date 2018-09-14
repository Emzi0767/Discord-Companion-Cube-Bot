-- Companion Cube database schema
-- for PostgreSQL 9.6+
-- 
-- Author:          Emzi0767
-- Project:         Companion Cube
-- Version:         2 to 3
-- Last update:     2018-07-19 11:04 +02:00
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

-- cc_musicenabled
-- This table contains guilds for which music playback is enabled.
create table cc_musicenabled(
    guild_id bigint primary key
);