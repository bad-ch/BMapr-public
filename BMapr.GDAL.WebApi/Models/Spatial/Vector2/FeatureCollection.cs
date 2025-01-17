using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Models.Spatial.Vector2
{
    public class FeatureCollection
    {
        [JsonProperty(PropertyName = "type")]
        public string Type = "FeatureCollection";

        [JsonProperty(PropertyName = "name")]
        public string Name;

        [JsonProperty(PropertyName = "crs")]
        public object Crs;

        [JsonProperty(PropertyName = "features")]
        public List<Feature> Features;

        public FeatureCollection()
        {
            Features = new List<Feature>();
        }
    }
}
