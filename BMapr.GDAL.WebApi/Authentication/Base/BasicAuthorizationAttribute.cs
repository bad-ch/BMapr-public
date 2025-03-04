using Microsoft.AspNetCore.Authorization;

namespace BMapr.GDAL.WebApi.Authentication.Base
{
    public class BasicAuthorizationAttribute : AuthorizeAttribute
    {
        public BasicAuthorizationAttribute()
        {
            Policy = "BasicAuthentication";
        }
    }
}
