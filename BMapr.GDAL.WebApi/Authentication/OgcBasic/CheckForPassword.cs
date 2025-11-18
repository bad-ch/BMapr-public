using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace BMapr.GDAL.WebApi.Authentication.OgcBasic
{
    public class CheckForPassword : IAsyncActionFilter
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly Config _config;

        public CheckForPassword()
        {
            _configuration = ServiceLocator.ServiceProvider.GetService<IConfiguration>();
            _environment = ServiceLocator.ServiceProvider.GetService<IWebHostEnvironment>();
            _config = ConfigService.Get(_configuration, _environment);
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var project = context.RouteData.Values["project"]?.ToString();

            if (project == null)
            {
                context.Result = new BadRequestObjectResult($"Project is missing");
                return;
            }

            var projectSettings = ProjectSettingsService.Get(project, _config);

            if (projectSettings != null && !string.IsNullOrEmpty(projectSettings.BasicAuthenticationUser) && !string.IsNullOrEmpty(projectSettings.BasicAuthenticationPassword))
            {
                var request = context.HttpContext.Request;
                var url = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
                context.Result = new RedirectResult(url.Replace("/Ogc/","/OgcSecure/"));
                return;
            }

            await next();
        }
    }
}
