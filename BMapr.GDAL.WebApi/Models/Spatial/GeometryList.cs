using BMapr.GDAL.WebApi.Services;

namespace BMapr.GDAL.WebApi.Models.Spatial
{
    public class GeometryList
    {
        public string Layer { get; set; }

        public List<Vector.GeometryWkt> Geometries { get; set; } = new List<Vector.GeometryWkt>();
        
        public Mapserver.LayerGeometryType Type { get; set; }
        
        public Style.Style Sld { get; set; }
        
        public int? Epsg { get; set; }

        public string MimeType { get; set; }

        public Extent Extent { get; set; }
        
        public int[] Size { get; set; }
        
        public int Scale { get; set; }
    }
}
