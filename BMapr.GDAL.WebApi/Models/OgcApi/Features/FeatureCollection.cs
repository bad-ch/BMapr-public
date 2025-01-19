using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Models.OgcApi.Features
{
    public class FeatureCollection
    {
        [JsonProperty(PropertyName = "type")]
        public string Type = "FeatureCollection";

        [JsonProperty(PropertyName = "name")]
        public string Name;

        [JsonProperty(PropertyName = "description")]
        public string Description;

        [JsonProperty(PropertyName = "crs")]
        public string Crs;

        [JsonProperty(PropertyName = "numberReturned")]
        public long NumberReturned = 0;

        [JsonProperty(PropertyName = "timeStamp")]
        public string timeStamp = DateTime.UtcNow.ToString("O");

        [JsonProperty(PropertyName = "features")]
        public List<Feature> Features;

        public FeatureCollection()
        {
            Features = new List<Feature>();
        }
    }
}
