using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace BMapr.GDAL.WebApi.Models.Tracking
{
    public class Location
    {
        [JsonPropertyName("_type")]
        [JsonProperty("_type")]
        public string Type { get; set; }

        [JsonPropertyName("acc")]
        [JsonProperty("acc")]
        public int Accuracy { get; set; }

        [JsonPropertyName("alt")]
        [JsonProperty("alt")]
        public int Altitude { get; set; }

        [JsonPropertyName("batt")]
        [JsonProperty("batt")]
        public int BatteryPercentage { get; set; }

        [JsonPropertyName("bs")]
        [JsonProperty("bs")]
        public int BatteryStatus { get; set; }

        [JsonPropertyName("conn")]
        [JsonProperty("conn")]
        public string Connectivity { get; set; }

        [JsonPropertyName("created_at")]
        [JsonProperty("created_at")]
        public int CreationDate { get; set; }

        [JsonPropertyName("lat")]
        [JsonProperty("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lon")]
        [JsonProperty("lon")]
        public double Longitude { get; set; }

        [JsonPropertyName("m")]
        [JsonProperty("m")]
        public int MonitoringMode { get; set; }

        [JsonPropertyName("tid")]
        [JsonProperty("tid")]
        public string Tid { get; set; }

        [JsonPropertyName("topic")]
        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonPropertyName("tst")]
        [JsonProperty("tst")]
        public int TimestampSecondsLocationFix { get; set; }

        [JsonPropertyName("vac")]
        [JsonProperty("vac")]
        public int VerticalAccuracy { get; set; }

        [JsonPropertyName("vel")]
        [JsonProperty("vel")]
        public int Velocity { get; set; }
    }
}
