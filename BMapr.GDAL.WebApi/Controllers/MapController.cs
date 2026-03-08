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
    public IActionResult GetImageFromMap(string project, [FromQuery] double xmin, [FromQuery] double ymin, [FromQuery] double xmax, [FromQuery] double ymax, [FromQuery] int width, [FromQuery] int height, [FromQuery] string format, [FromQuery] string epsg, [FromQuery] string layers, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        //todo handle epsg code

        Config.Host = HostService.Get(Request, IConfig);

        var mapMetadata = MapFileService.GetMapFromProject(project, Config);

        if (!mapMetadata.Succesfully || string.IsNullOrEmpty(mapMetadata.Value.Key) || string.IsNullOrEmpty(mapMetadata.Value.FilePath))
        {
            return new BadRequestResult();
        }

        cancellationToken.ThrowIfCancellationRequested();

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

        cancellationToken.ThrowIfCancellationRequested();

        //mapserver.SetLayerByName("sections", false);

        var imageBlueFeature = mapServer.DrawImage(format, width, height, cancellationToken);

        return new FileContentResult(imageBlueFeature, format);
    }
}