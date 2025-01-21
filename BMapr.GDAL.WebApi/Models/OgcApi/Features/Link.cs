using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace BMapr.GDAL.WebApi.Models.OgcApi.Features
{
    public class Link
    {
        [JsonProperty("rel")]
        [JsonPropertyName("rel")]
        public string Rel;

        [JsonProperty("type")]
        [JsonPropertyName("type")]
        public string Type;

        [JsonProperty("title")]
        [JsonPropertyName("title")]
        public string Title;

        [JsonProperty("href")]
        [JsonPropertyName("href")]
        public string Href;
    }
}
