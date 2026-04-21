using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Services;
using System.Text;

namespace BMapr.GDAL.WebApi.Authentication.OgcBasic
{
    public class BasicAuthenticationHandler
    {
        public bool ValidateCredentials(ProjectSettings projectSettings, string username, string password)
        {
            var user = projectSettings.Users.SingleOrDefault(x => x.Name == username);

            if (user == null)
            {
                return false;
            }

            return user.Password == password;
        }

        public bool IsAuthenticationSuccessfully(HttpContext context, ProjectSettings projectSettings, out string username)
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

                    if (ValidateCredentials(projectSettings, user, pass))
                    {
                        username = user;
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
