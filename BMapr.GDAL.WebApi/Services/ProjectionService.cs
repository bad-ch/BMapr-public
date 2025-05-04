namespace BMapr.GDAL.WebApi.Services
{
    public class ProjectionService
    {
        public static int getEPSGCode(string crs)
        {
            if (crs.ToLower() == "http://www.opengis.net/def/crs/ogc/1.3/crs84"  || crs.ToLower().Contains("wgs84") || crs.ToLower().Contains("crs84"))
            {
                return  4326;
            }
            else
            {
                return Convert.ToInt32(crs.ToLowerInvariant().Replace("http://www.opengis.net/def/crs/epsg/0/", ""));
            }
        }

        public static string getNameSpaceFromEPSG(int epsgCode)
        {
            if (epsgCode == 4326)
            {
                return "http://www.opengis.net/def/crs/OGC/1.3/CRS84";
            }

            return $"http://www.opengis.net/def/crs/epsg/0/{epsgCode}";
        }
    }
}
