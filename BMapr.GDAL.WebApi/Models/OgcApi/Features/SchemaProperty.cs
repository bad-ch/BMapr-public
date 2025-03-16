using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Models.OgcApi.Features
{
    public class SchemaProperty
    {
        public bool? ReadOnly { get; set; }

        [JsonProperty("$ref")]
        public string? Ref { get; set; }

        [JsonProperty("x-ogc-role")]
        public string? Role { get; set; } // id, primary-geometry

        public string? Title { get; set; }

        public List<object>? Enum { get; set; }

        public string? Format { get; set; } // date-time, geometry-point ..

        public string? Type { get; set; } // string, integer, number

        [JsonProperty("x-ogc-propertySeq")]
        public int? Sequence { get; set; }
    }
}
