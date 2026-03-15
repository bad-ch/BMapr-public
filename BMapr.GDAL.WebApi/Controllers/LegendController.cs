using BMapr.GDAL.WebApi.Authentication.OgcBasic;
using BMapr.GDAL.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using OSGeo.MapServer;
using System.Text.RegularExpressions;
using BMapr.GDAL.WebApi.Services;

namespace BMapr.GDAL.WebApi.Controllers;

[ApiController]
[BasicAuthIfPassword]
[Route("api/legend")]
public class LegendController : DefaultController
{
    private readonly ILogger<LegendController> _logger;

    public LegendController(ILogger<LegendController> logger, IConfiguration config, IWebHostEnvironment environment) : base(config, environment)
    {
        _logger = logger;
    }

    [HttpPost("{project}")]
    public IActionResult GetLegend(string project, [FromBody] PreviewRequest req)
    {
        if (req.Geometry.ToLower() == "point")
        {
           return File(LegendService.GetPointLegendDefinition(req.Width, req.Height, req.Style, req.Symbol, ""),"image/png");
        }
        if (req.Geometry.ToLower() == "polygon")
        {
            return File(LegendService.GetPolygonLegendDefinition(req.Width, req.Height, req.Style, req.Symbol, ""),"image/png");
        }
        if (req.Geometry.ToLower() == "line")
        {
            return File(LegendService.GetLineLegendDefinition(req.Width, req.Height, req.Style, req.Symbol, ""), "image/png");
        }

        throw new NotImplementedException($"Legend for geometry type {req.Geometry} is not implemented yet.");
    }


}