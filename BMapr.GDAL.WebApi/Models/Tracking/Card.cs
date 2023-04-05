using System.Text.Json.Serialization;
using LiteDB;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Models.Tracking
{
    public class Card
    {
        [BsonId]
        [JsonPropertyName("_id")]
        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonPropertyName("_type")]
        [JsonProperty("_type")]
        public string Type { get; set; }

        [JsonPropertyName("tid")]
        [JsonProperty("tid")]
        public string Tid { get; set; }

        /// <summary>
        /// base 64 encoded image of the friend, optimal size 192 x 192
        /// </summary>
        [JsonPropertyName("face")]
        [JsonProperty("face")]
        public string Face { get; set; }

        [JsonPropertyName("name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        public Card()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
