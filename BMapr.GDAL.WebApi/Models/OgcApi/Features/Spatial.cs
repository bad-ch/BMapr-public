using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace BMapr.GDAL.WebApi.Models.OgcApi.Features
{
    public class Spatial
    {
        [JsonProperty("bbox")]
        [JsonPropertyName("bbox")]
        public List<List<double>> Bbox = new ();

        [JsonProperty("crs")]
        [JsonPropertyName("crs")]
        public string Crs = string.Empty;
    }
}
