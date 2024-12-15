using BMapr.GDAL.WebApi.Services;

namespace BMapr.GDAL.WebApi.Models
{
    public class WebRequestParameter
    {
        public string Url { get; set; } = string.Empty;
        public WebRequestService.MethodType Method { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Accept { get; set; } = string.Empty;
        public string BodyContent { get; set; } = string.Empty;
        public string BodyContentType { get; set; } = string.Empty;
        public int TimeOut { get; set; } = 120000;
        public bool AllowAutoRedirect { get; set; } = true;

        public WebRequestParameter()
        {
            Headers = new Dictionary<string, string>();
        }
    }
}
