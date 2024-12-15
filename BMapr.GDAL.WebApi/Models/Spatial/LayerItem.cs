namespace BMapr.GDAL.WebApi.Models.Spatial
{
    public class LayerItem
    {
        public string Name { get; set; }
        public int Epsg { get; set; }
        public bool SpatialReferenceAvailable { get; set; }
        public Extent Extent { get; set; }
        public long FeatureCount { get; set; }

        public List<Property> Properties { get; set; }
        public List<string> GeometryTypes { get; set; }
        public List<object> GeometryTypesGdal { get; set; }
        public Dictionary<int, bool> OverlapSpatialRefs { get; set; }

        public LayerItem()
        {
            Properties = new List<Property>();
            GeometryTypes = new List<string>();
            GeometryTypesGdal = new List<object>();
            OverlapSpatialRefs = new Dictionary<int, bool>();
        }

    }
}
