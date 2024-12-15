using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.Spatial;
using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BMapr.GDAL.WebApi.Controllers
{
    /// <summary>
    /// Processing of GIS operations
    /// </summary>
    [ApiController]
    [Route("api/Processing")]
    public class ProcessingController : DefaultController
    {
        private readonly ILogger<ProcessingController> _logger;
        private readonly IMemoryCache _cache;

        public ProcessingController(ILogger<ProcessingController> logger, IConfiguration iConfig, IWebHostEnvironment environment, IMemoryCache cache) : base(iConfig, environment)
        {
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// Convert Wkt geometries to an image, styled with a json sld
        /// </summary>
        /// <param name="geometryList"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <response code="200">Returns an image, png or jpeg</response>
        [HttpPost("GetImageFromWkt")]
        public ActionResult GetImageFromWkt([FromBody] GeometryList geometryList, [FromQuery(Name = "token")] string? token)
        {
            if (!TokenService.Check(Request, IConfig, null, token))
            {
                return BadRequest("Process or system token invalid");
            }

            if (geometryList == null)
            {
                return BadRequest("Wrong body content");
            }

            //string body;

            //using (var reader = new StreamReader(Request.Body))
            //{
            //    body = reader.ReadToEnd();
            //}

            //if (string.IsNullOrEmpty(body))
            //{
            //    return BadRequest();
            //}

            //var geometryList = JsonConvert.DeserializeObject<GeometryList>(body);

            //if (geometryList == null)
            //{
            //    return BadRequest("Wrong body content");
            //}

            var style = geometryList.Sld;

            var sldDescription = geometryList.Sld.GetSldFromStyle(style, geometryList.Type, geometryList.Scale);

            var mapserver = new Mapserver();

            mapserver.AddLayer("test", Mapserver.LayerConnectionType.Inline, geometryList.Type);

            foreach (var geometry in geometryList.Geometries)
            {
                mapserver.AddGeometryToLayer("test", geometry.Wkt, geometry.Label ?? "");
            }

            mapserver.MapserverSetExtent(geometryList.Extent.Xmin, geometryList.Extent.Ymin, geometryList.Extent.Xmax, geometryList.Extent.Ymax, geometryList.Size[0], geometryList.Size[1]);

            mapserver.ApplySldDefinition("test", sldDescription);

            //todo error handling

            var imageByteArray = mapserver.MapserverDrawImage(geometryList.MimeType, geometryList.Size[0], geometryList.Size[1]); //take style from layer modified with sld

            return new FileContentResult(imageByteArray, geometryList.MimeType);
        }

        /// <summary>
        /// Push nc raster data to process extraction of latest band in a separate file and copy the data to the project, post form data
        /// </summary>
        /// <param name="requestParameter">multipart form, with one file and the form values token, project and filename</param>
        /// <returns></returns>
        [HttpPost("PushRasterData")]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public ActionResult PushRasterData([FromForm] RequestPushNcData requestParameter)
        {
            if (string.IsNullOrEmpty(requestParameter.Token) || string.IsNullOrEmpty(requestParameter.Project) || string.IsNullOrEmpty(requestParameter.FileName))
            {
                return BadRequest("token or project or filename is missing");
            }

            if (!TokenService.Check(Request, IConfig, null, requestParameter.Token))
            {
                return BadRequest("Process or system token invalid");
            }

            var dataProjects = Config.DataProjects?.FullName;

            if (dataProjects == null)
            {
                return BadRequest("Projects path is empty");
            }

            var file = requestParameter.File;
            byte[] content;

            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                content = ms.ToArray();
            }


            var dataProject = Path.Combine(dataProjects, requestParameter.Project);
            var filePath = Path.Combine(dataProject, requestParameter.FileName);

            var result = RasterDatasetService.ExtractBand(content, "nc", filePath);

            if (!result.Succesfully)
            {
                return BadRequest(string.Join(",", result.Messages));
            }

            return Ok();
        }
    }
}
