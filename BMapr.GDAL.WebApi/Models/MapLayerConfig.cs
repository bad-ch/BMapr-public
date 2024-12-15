namespace BMapr.GDAL.WebApi.Models
{
    public class MapLayerConfig
    {
        public bool WFSTuseMsSqlServerFeatureService { get; set; }
        public bool WFSTMsSqlServerGeography { get; set; }
        public bool IdSquenceManual { get; set; }
        public long IdSequenceStartValue { get; set; }
        public string Id { get; set; }
        public string IdType { get; set; }
        public string Connection { get; set; } // filepath or db connection
        public int EPSG { get; set; } = 0;
    }
}
