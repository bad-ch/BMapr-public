namespace BMapr.GDAL.WebApi.Models.OgcApi.Features
{
    public class CollectionItem
    {
        public static readonly string DEFAULT_CRS = "http://www.opengis.net/def/crs/OGC/1.3/CRS84";

        public string Name { get; set; } // collection id

        public string Title { get; set; }

        public string Description { get; set; }

        public string SpatialCrs { get; set; } = DEFAULT_CRS;

        public string StorageCrs { get; set; } = DEFAULT_CRS;

        public string DefaultCrs { get; set; } = DEFAULT_CRS;

        public List<string> AdditionalCrs { get; set; } = new ();

        public double MinX { get; set; }

        public double MinY { get; set; }

        public double MaxX { get; set; }

        public double MaxY { get; set; }
    }
}
