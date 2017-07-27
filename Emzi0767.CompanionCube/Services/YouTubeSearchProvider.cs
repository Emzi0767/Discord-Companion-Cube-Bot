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
            using (var req = await this.Http.GetAsync(uri))
            using (var res = await req.Content.ReadAsStreamAsync())
            using (var sr = new StreamReader(res, CompanionCubeUtilities.UTF8))
                json = await sr.ReadToEndAsync();

            var jsonData = JObject.Parse(json);
            var data = jsonData["items"].ToObject<IEnumerable<YouTubeApiResponseItem>>();

            return data.Select(x => new YouTubeSearchResult(x.Snippet.Title, x.Snippet.Author, x.Id.VideoId));
        }
    }
}
