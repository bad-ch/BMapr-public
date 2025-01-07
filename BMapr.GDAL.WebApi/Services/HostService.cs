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

            var xForwardedHost = request.Headers["X-Forwarded-Host"];
            var xForwardedProto = request.Headers["X-Forwarded-Proto"];

            if (!string.IsNullOrEmpty(xForwardedHost) && !string.IsNullOrEmpty(xForwardedProto))
            {
                return $"{xForwardedProto}://{xForwardedHost}{request.PathBase}";
            }

            return $"{request.Scheme}://{request.Host}{request.PathBase}";
        }
    }
}
