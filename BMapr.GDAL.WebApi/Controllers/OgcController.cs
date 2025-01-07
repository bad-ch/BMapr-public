using System.Text;
using BMapr.GDAL.WebApi.Controllers;
using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using OSGeo.MapServer;

namespace BMapr.WebApi.Controllers
{
    /// <summary>
    /// OCG interface for UMN MapServer
    /// </summary>
    [ApiController]
    [Route("api/Ogc")]
    public class OgcController : DefaultController
    {
        private readonly ILogger<OgcController> _logger;

        public OgcController(ILogger<OgcController> logger, IConfiguration iConfig, IWebHostEnvironment environment, IMemoryCache cache) : base(iConfig, environment)
        {
            _logger = logger;
        }

        /// <summary>
        /// Endpoint (GET) for handle WMS/ WFS and WMTS from uploaded mapfiles
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        [HttpGet("{project}")]
        [HttpHead("{project}")]
        //[EnableCors("AllowAnyOrigin")]
        public ActionResult Interface(string project)
        {
            //if (_cache.TryGetValue("aliasTable", out Dictionary<string, string> aliasTable) && aliasTable.ContainsKey(project))
            //{
            //    _logger.LogDebug($"OGC interface: alias {project} to {aliasTable[project]}");
            //    project = aliasTable[project];
            //}

            var queryString = HttpContext.Request.QueryString.ToString();

            if (string.IsNullOrEmpty(queryString))
            {
                return BadRequest("Missing parameters");
            }

            if (queryString.StartsWith("?"))
            {
                queryString = queryString.Substring(1);
            }

            Config.Host = HostService.Get(Request, IConfig);

            _logger.LogDebug($"OGC interface: {project}, {queryString}");

            if (queryString.ToLower().Contains("service=wmts"))
            {
                // special handling because Mapserver don't support WMTS
                return WmtsService.HandleRequest(project, queryString, Config);
            }

            return OgcService.Process( OgcService.RequestType.Get, queryString , "", Config, project,"");
        }

        /// <summary>
        /// Endpoint (POST) for handle WMS/ WFS and WMTS from uploaded mapfiles
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        [HttpPost("{project}")]
        //[HttpHead("{project}")]
        //[EnableCors("AllowAnyOrigin")]
        public ActionResult InterfacePost(string project)
        {
            //if (_cache.TryGetValue("aliasTable", out Dictionary<string, string> aliasTable) && aliasTable.ContainsKey(project))
            //{
            //    _logger.LogDebug($"OGC interface: alias {project} to {aliasTable[project]}");
            //    project = aliasTable[project];
            //}

            var bodyContent = Request.GetRawBodyStringAsync(Encoding.UTF8).Result;
            var bodyContentType = Request.ContentType ?? "";
            var bodyContentLower = bodyContent.ToLower();

            if (string.IsNullOrEmpty(bodyContent))
            {
                return BadRequest("Post request body is empty");
            }

            Config.Host = HostService.Get(Request, IConfig);

            if (bodyContentLower.Contains("service") && bodyContentLower.Contains("wfs") && bodyContentLower.Contains("transaction"))
            {
                return WfsService.Transaction(Config, project, bodyContent);
            }

            _logger.LogDebug($"OGC POST interface: {project}, {bodyContent}, {bodyContentType}");

            return OgcService.Process(OgcService.RequestType.Post, "", bodyContent, Config, project, bodyContentType);
        }

        [HttpGet("{project}/wmts/getcapabilities/{version}")]
        public ActionResult WmtsRest(string project, string version)
        {
            Config.Host = HostService.Get(Request, IConfig);

            return WmtsService.HandleRequest( project, $"SERVICE=WMTS&REQUEST=GetCapabilities&VERSION={version}", Config);
        }

        [HttpGet("{project}/wmts/{version}/{layer}/{style}/{format}/{tileMatrixSet}/{tileMatrix}/{tileRow}/{tileColumn}")]
        public ActionResult WmtsRestInterfaceV1(string project, string version, string layer, string style, string format, string tileMatrixSet, string tileMatrix, int tileRow, int tileColumn)
        {
            var request = "GetTile";
            return WmtsService.HandleRequest( project, $"SERVICE=WMTS&REQUEST={request}&VERSION={version}&LAYER={layer}&STYLE={style}&FORMAT={format}&TILEMATRIXSET={tileMatrixSet}&TILEMATRIX={tileMatrix}&TILEROW={tileRow}&TILECOL={tileColumn}", Config);
        }

        [HttpGet("{project}/wmts/{version}/{layer}/{style}/{tileMatrixSet}/{tileMatrix}/{tileRow}/{tileColumn}.{format}")]
        public ActionResult WmtsRestInterfaceV2(string project, string version, string layer, string style, string tileMatrixSet, string tileMatrix, int tileRow, int tileColumn, string format)
        {
            var request = "GetTile";
            return WmtsService.HandleRequest( project, $"SERVICE=WMTS&REQUEST={request}&VERSION={version}&LAYER={layer}&STYLE={style}&FORMAT={format}&TILEMATRIXSET={tileMatrixSet}&TILEMATRIX={tileMatrix}&TILEROW={tileRow}&TILECOL={tileColumn}", Config);
        }

        [HttpGet("{project}/wmts/{version}/{layer}/{style}/{dimension}/{format}/{tileMatrixSet}/{tileMatrix}/{tileRow}/{tileColumn}")]
        public ActionResult WmtsRestInterfaceV3(string project, string version, string layer, string style, string dimension, string format, string tileMatrixSet, string tileMatrix, int tileRow, int tileColumn)
        {
            var request = "GetTile";
            return WmtsService.HandleRequest( project, $"SERVICE=WMTS&REQUEST={request}&VERSION={version}&LAYER={layer}&STYLE={style}&DIMENSION={dimension}&FORMAT={format}&TILEMATRIXSET={tileMatrixSet}&TILEMATRIX={tileMatrix}&TILEROW={tileRow}&TILECOL={tileColumn}", Config);
        }

        [HttpGet("{project}/wmts/{version}/{layer}/{style}/{dimension}/{tileMatrixSet}/{tileMatrix}/{tileRow}/{tileColumn}.{format}")]
        public ActionResult WmtsRestInterfaceV4(string project, string version, string layer, string style, string dimension, string tileMatrixSet, string tileMatrix, int tileRow, int tileColumn, string format)
        {
            var request = "GetTile";
            return WmtsService.HandleRequest( project, $"SERVICE=WMTS&REQUEST={request}&VERSION={version}&LAYER={layer}&STYLE={style}&DIMENSION={dimension}&FORMAT={format}&TILEMATRIXSET={tileMatrixSet}&TILEMATRIX={tileMatrix}&TILEROW={tileRow}&TILECOL={tileColumn}", Config);
        }
    }
}