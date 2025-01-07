namespace BMapr.GDAL.WebApi.Services
{
    public static class HostService
    {
        public static string Get(HttpRequest request)
        {
            return $"{request.Scheme}://{request.Host}{request.PathBase}";
        }
    }
}
