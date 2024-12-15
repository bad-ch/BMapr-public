using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace BMapr.GDAL.WebApi.Models.Tracking
{
    public class LocationSkeleton
    {
        [JsonPropertyName("_type")]
        [JsonProperty("_type")]
        public string Type { get; set; }

        [JsonPropertyName("lat")]
        [JsonProperty("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lon")]
        [JsonProperty("lon")]
        public double Longitude { get; set; }

        [JsonPropertyName("tid")]
        [JsonProperty("tid")]
        public string Tid { get; set; }
        
        [JsonPropertyName("tst")]
        [JsonProperty("tst")]
        public int TimestampSecondsLocationFix { get; set; }
    }
}
