namespace BMapr.GDAL.WebApi.Models.Spatial
{
    public class FeatureBody
    {
        public object Id { get; set; }
        public string Geometry { get; set; }
        public Dictionary<string,object> Properties { get; set; }
        public int Epsg { get; set; } = 0;

        public FeatureBody()
        {
            Properties = new Dictionary<string,object>();
        }
    }
}
