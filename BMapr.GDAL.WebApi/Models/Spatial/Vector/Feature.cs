using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Models.Spatial.Vector
{
    public class Feature
    {
        [JsonProperty(PropertyName = "type")]
        public String Type = "Feature";

        [JsonProperty(PropertyName = "geometry")]
        public Geometry Geometry;

        [JsonProperty(PropertyName = "properties")]
        public Dictionary<string, object> Properties;

        public Feature()
        {
            Properties = new Dictionary<string, object>();
        }

        public void AddQueryField(Dictionary<string, string> fieldQueries)
        {
            foreach (var fieldQuery in fieldQueries)
            {
                string value = fieldQuery.Value;

                foreach (var property in this.Properties)
                {
                    value = value.Replace(String.Format("#{0}#", property.Key), property.Value.ToString());
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
