namespace BMapr.GDAL.WebApi.Models.Net
{
    public class RequestContent
    {
        public List<FileContent> Files { get; set; }
        public Dictionary<string, object> Values { get; set; }

        public RequestContent()
        {
            Files = new List<FileContent>();
            Values = new Dictionary<string, object>();
        }
    }
}
