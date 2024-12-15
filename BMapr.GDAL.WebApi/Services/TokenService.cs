using BMapr.GDAL.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace BMapr.GDAL.WebApi.Services
{
    public class TokenService
    {
        public static bool Check(HttpRequest request, IConfiguration iConfig, ProjectSettings? projectSettings, string urlToken)
        {
            request.Headers.TryGetValue("Authorization", out var headerToken);

            var systemToken = iConfig.GetSection("Settings").GetSection("Token").Value;

            if (
                (projectSettings != null && projectSettings.Token.ToString() == urlToken && projectSettings.Token != Guid.Empty) || 
                (projectSettings != null && projectSettings.Token.ToString() == headerToken && projectSettings.Token != Guid.Empty) ||
                systemToken == urlToken || 
                systemToken == headerToken 
            )
            {
                return true;
            }

            return false;
        }
    }
}
