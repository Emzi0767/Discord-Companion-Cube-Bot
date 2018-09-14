// This file is part of Companion Cube project
//
// Copyright 2018 Emzi0767
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
