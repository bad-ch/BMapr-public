using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Models.OgcApi.Features
{
    public class Feature
    {
        [JsonProperty(PropertyName = "type")]
        public string Type = "Feature";

        [JsonProperty(PropertyName = "id")]
        public string Id;

        [JsonProperty(PropertyName = "geometry")]
        public object Geometry;

        [JsonProperty(PropertyName = "properties")]
        public Dictionary<string, object> Properties = null;

        public Feature()
        {
            Properties = new Dictionary<string, object>();
        }

        public void AddQueryField(Dictionary<string, string> fieldQueries)
        {
            foreach (var fieldQuery in fieldQueries)
            {
                string value = fieldQuery.Value;

                foreach (var property in Properties)
                {
                    value = value.Replace(string.Format("#{0}#", property.Key), property.Value.ToString());
                }

                if (!Properties.ContainsKey(fieldQuery.Key))
                {
                    Properties.Add(fieldQuery.Key, value);
                }
            }
        }

        public void EncodeFields()
        {
            var fields = Properties.Select(property => property.Key).ToList();

            foreach (var field in fields)
            {
                Properties[field] = System.Net.WebUtility.HtmlEncode(Properties[field].ToString());
            }
        }
    }
}
