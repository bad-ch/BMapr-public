namespace BMapr.GDAL.WebApi.Models
{
    public class RequestPushNcData
    {
        public IFormFile File { get; set; }
        public string Token { get; set; }
        public string FileName { get; set; }
        public string Project { get; set; }
    }
}
