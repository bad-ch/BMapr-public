using Microsoft.AspNetCore.Authorization;

namespace BMapr.GDAL.WebApi
{
    public class BasicAuthorizationAttribute : AuthorizeAttribute
    {
        public BasicAuthorizationAttribute()
        {
            Policy = "BasicAuthentication";
        }
    }
}
