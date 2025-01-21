
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace BMapr.GDAL.WebApi.Models.OgcApi.Features
{
    public class LandingPage
    {
        [JsonProperty("title")]
        [JsonPropertyName("title")]
        public string Title;

        [JsonProperty("description")]
        [JsonPropertyName("description")]
        public string Description;

        [JsonProperty("links")]
        [JsonPropertyName("links")]
        public List<Link> Links;
    }
}
