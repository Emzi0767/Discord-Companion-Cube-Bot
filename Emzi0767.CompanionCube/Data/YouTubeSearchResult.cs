// This file is part of Companion Cube project
//
// Copyright (C) 2018-2021 Emzi0767
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using Newtonsoft.Json;

namespace Emzi0767.CompanionCube.Data
{
    /// <summary>
    /// Represents a YouTube search result.
    /// </summary>
    public struct YouTubeSearchResult
    {
        /// <summary>
        /// Gets the title of this item.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the name of the item's author.
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// Gets the item's ID.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Creates a new YouTube search result with specified parameters.
        /// </summary>
        /// <param name="title">Title of the item.</param>
        /// <param name="author">Item's author.</param>
        /// <param name="id">Item's ID.</param>
        public YouTubeSearchResult(string title, string author, string id)
        {
            this.Title = title;
            this.Author = author;
            this.Id = id;
        }
    }

    internal struct YouTubeApiResponseItem
    {
        [JsonProperty("id")]
        public ResponseId Id { get; private set; }

        [JsonProperty("snippet")]
        public ResponseSnippet Snippet { get; private set; }


        public struct ResponseId
        {
            [JsonProperty("videoId")]
            public string VideoId { get; private set; }
        }

        public struct ResponseSnippet
        {
            [JsonProperty("title")]
            public string Title { get; private set; }

            [JsonProperty("channelTitle")]
            public string Author { get; private set; }
        }
    }
}
