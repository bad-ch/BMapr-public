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

        [JsonProperty("description")]
        [JsonPropertyName("description")]
        public string Description;

        [JsonProperty("extent")]
        [JsonPropertyName("extent")]
        public Extent Extent;

        [JsonProperty("itemType")]
        [JsonPropertyName("itemType")]
        public string ItemType = "Feature";

        [JsonProperty("crs")]
        [JsonPropertyName("crs")]
        public List<string> Crs = new ();

        [JsonProperty("storageCrs")]
        [JsonPropertyName("storageCrs")]
        public string StorageCrs;

        [JsonProperty("links")] [JsonPropertyName("links")]
        public List<Link> Links = new();
    }
}
