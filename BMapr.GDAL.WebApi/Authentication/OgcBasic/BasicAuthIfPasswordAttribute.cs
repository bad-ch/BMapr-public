using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BMapr.GDAL.WebApi.Authentication.OgcBasic
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class BasicAuthIfPasswordAttribute : Attribute, IAuthorizationFilter
    {
        private readonly BasicAuthenticationHandler _basicAuthenticationHandler;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly Config _config;

        public BasicAuthIfPasswordAttribute()
        {
            _configuration = ServiceLocator.ServiceProvider.GetService<IConfiguration>();
            _environment = ServiceLocator.ServiceProvider.GetService<IWebHostEnvironment>();
            _config = ConfigService.Get(_configuration, _environment);
            _basicAuthenticationHandler = new BasicAuthenticationHandler();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var project = context.RouteData.Values["project"]?.ToString();

            if (project == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var projectSettings = ProjectSettingsService.Get(project, _config);

            if (projectSettings != null && !string.IsNullOrEmpty(projectSettings.BasicAuthenticationUser) && !string.IsNullOrEmpty(projectSettings.BasicAuthenticationPassword) && !_basicAuthenticationHandler.IsAuthenticationSuccessfully(context.HttpContext, projectSettings, out var username))
            {
                context.HttpContext.Items["Username"] = username;
                context.Result = new UnauthorizedResult();
                return;
            }
        }
    }
}
