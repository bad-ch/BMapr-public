using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace BMapr.GDAL.WebApi.Controllers;

[ApiController]
[Route("api/Data")]
public class DataController : DefaultController
{

    private readonly ILogger<DataController> _logger;

    public DataController(ILogger<DataController> logger, IConfiguration iConfig, IWebHostEnvironment environment)
        : base(iConfig, environment)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get file as partial stream
    /// </summary>
    /// <returns>Deliver Protomap tiles data</returns>
    [HttpGet("{project}/{file}/pmtiles")]
    public ActionResult GetPmTile(string project, string file)
    {
        var dataPath = Config.DataProject(project);
        var filePath = Path.Combine(dataPath.FullName, HttpUtility.UrlDecode(file));

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound($"file {file} not found");
        }

        var mimeType = FileService.GetMimeType(filePath);
        var extension = FileService.GetExtension(filePath);

        if (mimeType == null || extension != ".pmtiles")
        {
            return BadRequest($"only pm tiles are supported");
        }

        return File(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read), "application/octet-stream", true);
    }

    /// <summary>
    /// Get file as partial stream, cloud optimized geotif
    /// </summary>
    /// <returns>Deliver COT Cloud Optimized GeoTiff data</returns>
    [HttpGet("{project}/{file}/tif")]
    public ActionResult GetCot(string project, string file)
    {
        var dataPath = Config.DataProject(project);
        var filePath = Path.Combine(dataPath.FullName, HttpUtility.UrlDecode(file));

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound($"file {file} not found");
        }

        var mimeType = FileService.GetMimeType(filePath);
        var extension = FileService.GetExtension(filePath);

        if (mimeType == null || extension != ".tif")
        {
            return BadRequest($"only tif tiles are supported");
        }

        return File(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read), "application/octet-stream", true);
    }
}