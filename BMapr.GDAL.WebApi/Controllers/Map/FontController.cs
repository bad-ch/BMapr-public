using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BMapr.GDAL.WebApi.Controllers.Map;

[ApiController]
[Route("api/map/font")]
public class FontController(
    ILogger<FontController> logger,
    IConfiguration config,
    IWebHostEnvironment environment)
    : DefaultController(config, environment)
{
    private readonly ILogger<FontController> _logger = logger;

    [HttpPost("generate")] public IActionResult GenerateFontMetadata()
    {
        var filePath = Path.Combine(Config.DataShare.FullName, "font.list");
        var fileContent = System.IO.File.ReadAllBytes(filePath);

        FontService.GenerateFontMetadata(
            FontService.Load(filePath),
            Path.Combine(Config.DataShare.FullName, "fonts"));

        return Ok();
    }
}