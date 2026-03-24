using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.Map;
using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BMapr.GDAL.WebApi.Controllers.Map;

[ApiController]
[Route("api/map/library")]
public class LibraryController(
    ILogger<LegendController> logger,
    IConfiguration config,
    IWebHostEnvironment environment)
    : DefaultController(config, environment)
{
    private readonly ILogger<LegendController> _logger = logger;

    [HttpGet("symbol")]
    public IActionResult GetLibrary()
    {
        var filePath = Path.Combine(Config.DataShare.FullName, "library", "symbols.json");
        var fileContent = System.IO.File.ReadAllBytes(filePath);
        return new FileContentResult(fileContent, "application/json");
    }
}