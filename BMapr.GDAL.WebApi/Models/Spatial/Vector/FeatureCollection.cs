using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BMapr.GDAL.WebApi.Models.Spatial.Vector
{
    public class FeatureCollection
    {
        [JsonProperty(PropertyName = "type")]
        public string Type = "FeatureCollection";

        [JsonProperty(PropertyName = "features")]
        public List<Feature> Features;

        public FeatureCollection()
        {
            Features = new List<Feature>();
        }
    }
}
