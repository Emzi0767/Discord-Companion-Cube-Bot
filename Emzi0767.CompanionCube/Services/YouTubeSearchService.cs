using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Emzi0767.CompanionCube.Services
{
    public sealed class YouTubeSearchService
    {
        private static UTF8Encoding UTF8 { get; } = new UTF8Encoding(false);

        private string ApiKey { get; }
        private HttpClient Http { get; }

        public YouTubeSearchService(CompanionCubeYouTubeConfig cfg)
        {
            this.ApiKey = cfg.ApiKey;
            this.Http = new HttpClient()
            {
                BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/search")
            };
            this.Http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Companion-Cube");
        }

        public async Task<IEnumerable<YouTubeSearchResult>> SearchAsync(string term)
        {
            var uri = new Uri($"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=5&type=video&fields=items(id(videoId),snippet(title,channelTitle))&key={this.ApiKey}&q={WebUtility.UrlEncode(term)}");

            var json = "{}";
            using (var req = await this.Http.GetAsync(uri).ConfigureAwait(false))
            using (var res = await req.Content.ReadAsStreamAsync())
            using (var sr = new StreamReader(res, UTF8))
                json = await sr.ReadToEndAsync();

            var jsonData = JObject.Parse(json);
            var data = jsonData["items"].ToObject<IEnumerable<YouTubeApiResponseItem>>();

            return data.Select(x => new YouTubeSearchResult(x.Snippet.Title, x.Snippet.Author, x.Id.VideoId));
        }
    }

    public struct YouTubeSearchResult
    {
        public string Title { get; }
        public string Author { get; }
        public string Id { get; }

        public YouTubeSearchResult(string title, string author, string id)
        {
            this.Title = title;
            this.Author = author;
            this.Id = id;
        }
    }

    public struct YouTubeApiResponseItem
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
