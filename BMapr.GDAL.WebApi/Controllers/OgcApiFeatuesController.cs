using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.OgcApi.Features;
using BMapr.GDAL.WebApi.Services;
using BMapr.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Controllers
{
    /// <summary>
    /// OCG API features interface for UMN MapServer
    /// </summary>
    [ApiController]
    [Route("api/ogcapi/features")]
    public class OgcApiFeatuesController : DefaultController
    {
        private readonly ILogger<OgcController> _logger;

        public OgcApiFeatuesController(ILogger<OgcController> logger, IConfiguration iConfig, IWebHostEnvironment environment, IMemoryCache cache) : base(iConfig, environment)
        {
            _logger = logger;
        }

        /// <summary>
        /// Endpoint (GET) for landing page
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        [HttpGet("{project}")]
        [HttpHead("{project}")]
        [HttpGet("{project}/landingpage")]
        [HttpHead("{project}/landingpage")]
        public ActionResult LandingPage(string project,[FromQuery] string f = "application/json")
        {
            // no alternate format supported

            if (f.ToLower() != "application/json")
            {
                return BadRequest("OGC API format not supported");
            }

            var projectPath = Path.Combine(Config.DataProjects.FullName, project);

            if (!System.IO.Directory.Exists(projectPath))
            {
                return BadRequest("OGC API project not found");
            }

            Config.Host = HostService.Get(Request, IConfig);

            var landingPage = new LandingPage()
            {
                Title = "OGC API features service with OGR/GDAL/Mapserver",
                Description = "OGC API features service with OGR/GDAL/Mapserver",
                Links = new List<Link>(){
                    new() {
                        Rel = "service",
                        Type = "application/json",
                        Title = "API definition for this enpoint as JSON",
                        Href = $"{Config.Host}/api/ogcapi/features/{project}/?f=application/json"
                    },
                    new()
                    {
                        Rel = "service",
                        Type = "application/json",
                        Title = "API definition for this enpoint as JSON",
                        Href = $"{Config.Host}/api/ogcapi/features/{project}"
                    },
                    new() {
                        Rel = "service",
                        Type = "application/json",
                        Title = "API definition for this enpoint as JSON",
                        Href = $"{Config.Host}/api/ogcapi/features/{project}/landingpage?f=application/json"
                    },
                    new()
                    {
                        Rel = "service",
                        Type = "application/json",
                        Title = "API definition for this enpoint as JSON",
                        Href = $"{Config.Host}/api/ogcapi/features/{project}/landingpage"
                    },
                    new()
                    {
                        Rel = "conformance",
                        Type = "application/json",
                        Title = "Conformance Declaration as JSON",
                        Href = $"{Config.Host}/api/ogcapi/features/{project}/conformance"
                    },
                    new()
                    {
                        Rel = "conformance",
                        Type = "application/json",
                        Title = "Conformance Declaration as JSON",
                        Href = $"{Config.Host}/api/ogcapi/features/{project}/conformance?f=application/json"
                    },
                    new()
                    {
                        Rel = "data",
                        Type = "application/json",
                        Title = "Collections Metadata as JSON",
                        Href = $"{Config.Host}/api/ogcapi/features/{project}/collections"
                    },
                    new()
                    {
                        Rel = "data",
                        Type = "application/json",
                        Title = "Collections Metadata as JSON",
                        Href = $"{Config.Host}/api/ogcapi/features/{project}/collections?f=application/json"
                    },
                    new()
                    {
                        Rel = "self",
                        Type = "application/json",
                        Title = "This document",
                        Href = $"{Config.Host}/api/ogcapi/features/{project}"
                    },
                    //new()
                    //{
                    //    Rel = "alternate",
                    //    Type = "application/xml",
                    //    Title = "This Document as XML",
                    //    Href = $"{Config.Host}/api/ogcapi/features/{project}?f=application/xml"
                    //},
                }
            };

            return Ok(JsonConvert.SerializeObject(landingPage));
        }

        /// <summary>
        /// Endpoint (GET) for landing page
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        [HttpGet("{project}/conformance")]
        [HttpHead("{project}/conformance")]
        public ActionResult Conformance(string project, [FromQuery] string f = "application/json")
        {
            // no alternate format supported

            if (f.ToLower() != "application/json")
            {
                return BadRequest("OGC API format not supported");
            }

            var projectPath = Path.Combine(Config.DataProjects.FullName, project);

            if (!System.IO.Directory.Exists(projectPath))
            {
                return BadRequest("OGC API project not found");
            }

            Config.Host = HostService.Get(Request, IConfig);

            return Ok(new
            {
                conformsTo = new List<string>() {
                    "http://www.opengis.net/spec/ogcapi-features-1/1.1/conf/core",
                    "http://www.opengis.net/spec/ogcapi-features-1/1.1/conf/geojson"
                }
            });
        }
    }
}
