using BMapr.GDAL.WebApi.Authentication.OgcBasic;
using BMapr.GDAL.WebApi.Controllers;
using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Services;
using BMapr.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace BMapr.GDAL.WebApi.Controllers
{
    /// <summary>
    /// OCG interface for UMN MapServer
    /// </summary>
    [ApiController]
    [BasicAuth]
    [Route("api/OgcSecure")]
    public class OgcSecureController(
        ILogger<OgcController> logger,
        IConfiguration iConfig,
        IWebHostEnvironment environment,
        IMemoryCache cache)
        : DefaultController(iConfig, environment)
    {
        private readonly ILogger<OgcController> _logger = logger;
        private readonly OgcController _ogcController = new(logger, iConfig, environment, cache);

        [HttpGet("{project}")]
        [HttpHead("{project}")]
        public ActionResult Interface(string project)
        {
            _ogcController.ControllerContext = this.ControllerContext;
            return _ogcController.Interface(project);
        }

        [HttpPost("{project}")]
        public ActionResult InterfacePost(string project)
        {
            _ogcController.ControllerContext = this.ControllerContext;
            return _ogcController.InterfacePost(project);
        }
    }
}
