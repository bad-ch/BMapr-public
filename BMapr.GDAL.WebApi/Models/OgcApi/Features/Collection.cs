using BMapr.GDAL.WebApi.Models.Spatial;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace BMapr.GDAL.WebApi.Models.OgcApi.Features
{
    public class Collection
    {
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public string Id;

        [JsonProperty("title")]
        [JsonPropertyName("title")]
        public string Title;

        [JsonProperty("extent")]
        [JsonPropertyName("extent")]
        public Extent Extent;

        [JsonProperty("links")]
        [JsonPropertyName("links")]
        public List<Link> Links;
    }
}
