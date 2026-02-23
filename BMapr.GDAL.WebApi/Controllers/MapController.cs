using BMapr.GDAL.WebApi.Authentication.OgcBasic;
using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BMapr.GDAL.WebApi.Controllers;

[ApiController]
[BasicAuthIfPassword]
[Route("api/Map")]
public class MapController : DefaultController
{
    private readonly ILogger<MapController> _logger;

    public MapController(ILogger<MapController> logger, IConfiguration iConfig, IWebHostEnvironment environment, IMemoryCache cache) : base(iConfig, environment)
    {
        _logger = logger;
    }

    /// <summary>
    /// Endpoint get image from mapfile
    /// </summary>
    /// <param name="project"></param>
    /// <returns></returns>
    [HttpGet("{project}")]
    [HttpHead("{project}")]
    [EnableCors("AllowAnyOrigin")]
    public ActionResult GetImageFromMap(string project, [FromQuery] double xmin, [FromQuery] double ymin, [FromQuery] double xmax, [FromQuery] double ymax, [FromQuery] int width, [FromQuery] int height, [FromQuery] string format, [FromQuery] string epsg, [FromQuery] string layers)
    {
        //todo handle epsg code

        Config.Host = HostService.Get(Request, IConfig);

        var mapMetadata = MapFileService.GetMapFromProject(project, Config);

        if (!mapMetadata.Succesfully || string.IsNullOrEmpty(mapMetadata.Value.Key) || string.IsNullOrEmpty(mapMetadata.Value.FilePath))
        {
            return new BadRequestResult();
        }

        var resultMapConfig = MapFileService.GetMapConfigFromCache(mapMetadata.Value.Key, mapMetadata.Value.FilePath, Config, project);

        if (!resultMapConfig.Succesfully)
        {
            return new BadRequestResult();
        }

        var mapConfig = resultMapConfig.Value;

        GdalConfiguration.ConfigureGdal();
        GdalConfiguration.ConfigureOgr();

        var mapServer = new Mapserver(new FileInfo(mapConfig.MapPath));

        // todo check projection, format, and layers

        mapServer.SetExtent(xmin, ymin, xmax, ymax, width, height);

        //mapserver.SetLayerByName("sections", false);

        var imageBlueFeature = mapServer.DrawImage(format, width, height);

        return new FileContentResult(imageBlueFeature, format);
    }
}