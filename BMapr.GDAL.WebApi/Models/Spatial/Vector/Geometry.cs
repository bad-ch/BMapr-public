using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BMapr.GDAL.WebApi.Models.Spatial.Vector
{
    public class Geometry
    {
        //todo polygon supports only exterior rings
        //todo add support for multipart features

        [JsonProperty(PropertyName = "type")]
        public string Type;

        [JsonProperty(PropertyName = "coordinates")]
        public object Coordinates;

        public void AddPoint(Double x, Double y)
        {
            Type = "Point";
            Coordinates = new double[] { x, y };
        }

        public void AddLineString(List<double[]> points)
        {
            Type = "LineString";
            Coordinates = points.ToArray();
        }

        public void AddPolygon(List<double[]> points)
        {
            var rings = new object[] { points.ToArray() };

            Type = "Polygon";
            Coordinates = rings;
        }
    }
}
