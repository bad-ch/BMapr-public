using BMapr.GDAL.WebApi.Models.Map;
using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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

    [HttpGet("library")]
    public IActionResult GetLibrary()
    {
        var libraryPath = Path.Combine(Config.DataShare.FullName, "fonts");

        if (!Directory.Exists(libraryPath))
        {
            return NotFound("Font library not found. Please generate it first.");
        }

        var folders = new DirectoryInfo(libraryPath).GetDirectories().ToList();
        var fontsLibrary = new List<FontItem>();

        folders.ForEach(folder =>
        {
            var fontList = Path.Combine(folder.FullName, "font-list.json");

            if (!System.IO.File.Exists(fontList))
            {
                _logger.LogWarning("Font metadata not found for '{FontName}' (expected at: {FontListPath})", folder.Name, fontList);
                return;
            }

            var fontCharacters = JsonConvert.DeserializeObject<List<FontCharacterItem>>(System.IO.File.ReadAllText(fontList));

            if(fontCharacters == null)
            {
                _logger.LogWarning("Font metadata is empty for '{FontName}' (expected at: {FontListPath})", folder.Name, fontList);
                return;
            }

            fontsLibrary.Add(new FontItem()
            {
                Name = folder.Name,
                Characters = fontCharacters
            });
        });

        return Ok(fontsLibrary);
    }

    [HttpPost("generate")] 
    public IActionResult GenerateFontMetadata()
    {
        var filePath = Path.Combine(Config.DataShare.FullName, "font.list");

        FontService.GenerateFontMetadata(
            FontService.Load(filePath),
            Path.Combine(Config.DataShare.FullName, "fonts"));

        return Ok();
    }
}