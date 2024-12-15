using BMapr.GDAL.WebApi.Models;

namespace BMapr.GDAL.WebApi.Services
{
    public class CacheService
    {
        public static bool RequestCached(Config config, MapConfig mapConfig, string serviceParameter, string requestParameter)
        {
            if (config.Cache != null && !(bool)config.Cache)
            {
                return false;
            }

            if (serviceParameter == "wfs")
            {
                switch (requestParameter)
                {
                    case "getcapabilities":
                    case "describefeaturetype":
                        return mapConfig.CacheWfsMetadata;
                    case "getfeature":
                        return mapConfig.CacheWfsGetFeature;
                    default:
                        return false;
                }
            }

            if (serviceParameter == "wms")
            {
                switch (requestParameter)
                {
                    case "getcapabilities":
                        return mapConfig.CacheWmsGetCapabilities;
                    case "getmap":
                        return mapConfig.CacheWmsGetMap;
                    default:
                        return false;
                }
            }

            return false;
        }
    }
}
