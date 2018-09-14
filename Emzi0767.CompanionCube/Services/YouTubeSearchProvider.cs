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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Emzi0767.CompanionCube.Data;
using Newtonsoft.Json.Linq;

namespace Emzi0767.CompanionCube.Services
{
    /// <summary>
    /// Provides ability to search YouTube in a streamlined manner.
    /// </summary>
    public sealed class YouTubeSearchProvider
    {
        private string ApiKey { get; }
        private HttpClient Http { get; }

        /// <summary>
        /// Creates a new YouTube search provider service instance.
        /// </summary>
        /// <param name="cfg">Configuration of this service.</param>
        public YouTubeSearchProvider(CompanionCubeConfigYouTube cfg)
        {
            this.ApiKey = cfg.ApiKey;
            this.Http = new HttpClient()
            {
                BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/search")
            };
            this.Http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Companion-Cube");
        }

        /// <summary>
        /// Performs a YouTube search and returns the results.
        /// </summary>
        /// <param name="term">What to search for.</param>
        /// <returns>A collection of search results.</returns>
        public async Task<IEnumerable<YouTubeSearchResult>> SearchAsync(string term)
        {
            var uri = new Uri($"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=5&type=video&fields=items(id(videoId),snippet(title,channelTitle))&key={this.ApiKey}&q={WebUtility.UrlEncode(term)}");

            var json = "{}";
            using (var req = await this.Http.GetAsync(uri).ConfigureAwait(false))
            using (var res = await req.Content.ReadAsStreamAsync())
            using (var sr = new StreamReader(res, CompanionCubeUtilities.UTF8))
                json = await sr.ReadToEndAsync();

            var jsonData = JObject.Parse(json);
            var data = jsonData["items"].ToObject<IEnumerable<YouTubeApiResponseItem>>();

            return data.Select(x => new YouTubeSearchResult(x.Snippet.Title, x.Snippet.Author, x.Id.VideoId));
        }
    }
}
