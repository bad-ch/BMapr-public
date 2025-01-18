using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace BMapr.GDAL.WebApi.Models.OgcApi.Features
{
    public class Temporal
    {
        [JsonProperty("interval")]
        [JsonPropertyName("interval")]
        public List<List<DateTime>> Interval = new ();

        [JsonProperty("trs")]
        [JsonPropertyName("trs")]
        public string Trs = string.Empty;
    }
}
