namespace BMapr.GDAL.WebApi.Models.Spatial
{
    public class FeatureList
    {
        public string IdFieldName { get; set; } = "ID";
        public string GeometryFieldName { get; set; } = "SHAPE";
        public List<FeatureBody> Bodies { get; set; }

        public FeatureList()
        {
            Bodies = new List<FeatureBody>();
        }
    }
}
