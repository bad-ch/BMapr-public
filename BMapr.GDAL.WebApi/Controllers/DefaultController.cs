using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BMapr.GDAL.WebApi.Controllers
{
    public class DefaultController : ControllerBase
    {
        public readonly IConfiguration IConfig;
        public readonly Config Config;
        public IWebHostEnvironment Environment;

        public DefaultController(IConfiguration iConfig, IWebHostEnvironment environment)
        {
            IConfig = iConfig;
            Environment = environment;
            Config = ConfigService.Get(iConfig, environment);
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-CH");
        }
    }
}
