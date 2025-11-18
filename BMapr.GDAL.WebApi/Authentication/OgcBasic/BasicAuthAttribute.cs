using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BMapr.GDAL.WebApi.Authentication.OgcBasic
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class BasicAuthAttribute : Attribute, IAuthorizationFilter
    {
        private readonly BasicAuthenticationHandler _basicAuthenticationHandler;

        public BasicAuthAttribute()
        {
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

            if (!_basicAuthenticationHandler.IsAuthenticationSuccessfully(context.HttpContext, project, out var username))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            context.HttpContext.Items["Username"] = username;
        }
    }
}
