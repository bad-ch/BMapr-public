using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Services;
using System.Text;

namespace BMapr.GDAL.WebApi.Authentication.OgcBasic
{
    public class BasicAuthenticationHandler
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly Config _config;

        public BasicAuthenticationHandler()
        {
            _configuration = ServiceLocator.ServiceProvider.GetService<IConfiguration>();
            _environment = ServiceLocator.ServiceProvider.GetService<IWebHostEnvironment>();
            _config = ConfigService.Get(_configuration, _environment);
        }

        public bool ValidateCredentials(string project, string username, string password)
        {
            var projectSettings = ProjectSettingsService.Get(project, _config);

            if (projectSettings == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(projectSettings.BasicAuthenticationUser) ||
                string.IsNullOrEmpty(projectSettings.BasicAuthenticationPassword))
            {
                return false;
            }

            return username == projectSettings.BasicAuthenticationUser && password == projectSettings.BasicAuthenticationPassword;
        }

        public bool IsAuthenticationSuccessfully(HttpContext context, string project, out string username)
        {
            username = null;
            string authHeader = context.Request.Headers["Authorization"];

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
            {
                return false;
            }

            try
            {
                var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
                var decodedBytes = Convert.FromBase64String(encodedCredentials);
                var decodedCredentials = Encoding.UTF8.GetString(decodedBytes);

                var parts = decodedCredentials.Split(':');
                if (parts.Length == 2)
                {
                    var user = parts[0];
                    var pass = parts[1];

                    if (ValidateCredentials(project, user, pass))
                    {
                        username = user;
                        return true;
                    }
                }
            }
            catch
            {
                // Handle decoding issues
                return false;
            }

            return false;
        }
    }
}
