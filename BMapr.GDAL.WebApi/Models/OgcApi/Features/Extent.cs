using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace BMapr.GDAL.WebApi.Models.OgcApi.Features
{
    public class Extent
    {
        [JsonProperty("spatial")]
        [JsonPropertyName("spatial")]
        public Spatial Spatial;

        [JsonProperty("temporal")]
        [JsonPropertyName("temporal")]
        public Temporal Temporal;
    }
}
