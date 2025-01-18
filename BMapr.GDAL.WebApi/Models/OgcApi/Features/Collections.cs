using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace BMapr.GDAL.WebApi.Models.OgcApi.Features
{
    public class Collections
    {
        [JsonProperty("links")]
        [JsonPropertyName("links")]
        public List<Link> Links;

        [JsonProperty("collections")]
        [JsonPropertyName("collections")]
        public List<Collection> CollectionList;
    }
}
