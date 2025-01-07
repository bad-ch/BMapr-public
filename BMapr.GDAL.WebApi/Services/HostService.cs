namespace BMapr.GDAL.WebApi.Services
{
    public static class HostService
    {
        public static string Get(HttpRequest request, IConfiguration iConfig)
        {
            string host = iConfig.GetSection("Settings").GetSection("Host").Value;

            if (!string.IsNullOrEmpty(host))
            {
                return host;
            }

            return $"{request.Scheme}://{request.Host}{request.PathBase}";
        }
    }
}
