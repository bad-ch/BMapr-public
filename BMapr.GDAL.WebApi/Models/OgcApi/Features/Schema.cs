using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Models.OgcApi.Features
{
    public class Schema
    {
        public string Type { get; set; } = "object";

        public string Title { get; set; }

        public Dictionary<string, SchemaProperty> Properties { get; set; } = new();

        [JsonProperty("$schema")] 
        public string SchemaRef { get; set; } = "http://json-schema.org/draft/2019-09/schema";

        [JsonProperty("$id")]
        public string Id { get; set; }
    }
}
