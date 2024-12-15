namespace BMapr.GDAL.WebApi.Models
{
    public class MapConfig
    {
        public string MapPath { get; set; }

        public bool WFST110enabled { get; set; }
        public bool WFST110debug { get; set; }

        public bool CacheWmsGetCapabilities { get; set; }
        public bool CacheWmsGetMap { get; set; }
        public string CacheWmsGetMapPath { get; set; }

        public bool CacheWfsMetadata { get; set; } //includes GetCapabilities, DescribeFeatureType
        public bool CacheWfsGetFeature { get; set; }

        public Dictionary<string, MapLayerConfig> MapLayers { get; set; }

        public MapConfig()
        {
            MapLayers = new Dictionary<string, MapLayerConfig>();
        }

        public MapLayerConfig GetLayerConfig(string layerName)
        {
            if (string.IsNullOrEmpty(layerName) || !MapLayers.ContainsKey(layerName))
            {
                throw new Exception($"layer <<{layerName}>>not found in MapLayerConfig");
            }

            return MapLayers[layerName];
        }
    }
}
