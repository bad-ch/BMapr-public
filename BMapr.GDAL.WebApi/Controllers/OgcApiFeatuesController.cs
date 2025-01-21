using System.Text;
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

            var content = JsonConvert.SerializeObject(landingPage);

            return new FileContentResult(Encoding.UTF8.GetBytes(content), "application/json");
        }

        /// <summary>
        /// Endpoint (GET) conformance page
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

            var conformance = new
            {
                conformsTo = new List<string>()
                {
                    "http://www.opengis.net/spec/ogcapi-features-1/1.1/conf/core",
                    "http://www.opengis.net/spec/ogcapi-features-1/1.1/conf/geojson"
                }
            };

            var content = JsonConvert.SerializeObject(conformance);

            return new FileContentResult(Encoding.UTF8.GetBytes(content), "application/json");
        }

        /// <summary>
        /// Endpoint (GET) collections page
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        [HttpGet("{project}/collections")]
        [HttpHead("{project}/collections")]
        public ActionResult Collections(string project, [FromQuery] string f = "application/json")
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

            var collections = new Collections();
            var urlCollections = $"{Config.Host}/api/ogcapi/features/{project}/collections";

            collections.Links.Add(new Link()
            {
                Rel = "self",
                Title = "This document",
                Type = "application/json",
                Href = $"{urlCollections}?f=application/json"
            });

            var result = OgcApiFeaturesService.GetToc(Config, project, collections, urlCollections);
            var content = JsonConvert.SerializeObject(result.Value);

            return new FileContentResult(Encoding.UTF8.GetBytes(content), "application/json");
        }

        /// <summary>
        /// Endpoint (GET) for feature/data page
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        [HttpGet("{project}/collections/{collectionId}")]
        [HttpHead("{project}/collections/{collectionId}")]
        public ActionResult Collection(string project, string collectionId, [FromQuery] string f = "application/json")
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

            var urlCollections = $"{Config.Host}/api/ogcapi/features/{project}/collections";
            var result = OgcApiFeaturesService.GetCollection(Config, project, collectionId, urlCollections);
            var content = JsonConvert.SerializeObject(result.Value);

            return new FileContentResult(Encoding.UTF8.GetBytes(content), "application/json");
        }

        /// <summary>
        /// Endpoint (GET) for feature/data page
        /// </summary>
        /// <param name="project">name of project</param>
        /// <param name="collectionId">name of feature</param>
        /// <param name="bbox">envelope of the wished area</param>
        /// <param name="bboxCrs">CRS from the bbox if differnt from default</param>
        /// <param name="query">query of attributes</param>
        /// <param name="offset">paging start offset to skip</param>
        /// <param name="limit">page size</param>
        /// <param name="f">format of the queried data</param>
        /// <param name="file">download data as file</param>
        /// <returns></returns>
        [HttpGet("{project}/collections/{collectionId}/items")]
        [HttpHead("{project}/collections/{collectionId}/items")]
        [HttpOptions("{project}/collections/{collectionId}/items")]
        public ActionResult Feature(string project, string collectionId, [FromQuery] string? bbox, [FromQuery] string? bboxCrs, [FromQuery] string? query, [FromQuery] int? offset, [FromQuery] int? limit, [FromQuery] string f = "geojson", [FromQuery] bool file = false)
        {
            if (f.ToLower() != "geojson")
            {
                return BadRequest("OGC API format not supported");
            }

            var projectPath = Path.Combine(Config.DataProjects!.FullName, project);

            if (!Directory.Exists(projectPath))
            {
                return BadRequest("OGC API project not found");
            }

            Config.Host = HostService.Get(Request, IConfig);

            // todo check collection id exists
            if (string.IsNullOrEmpty(collectionId))
            {
                return BadRequest("OGC API collection id is empty");
            }

            if (limit != null && (limit < 0 || limit > 100000)) // max specification 10'000
            {
                return BadRequest("OGC API if set limit has to be between 1 and 100'000");
            }

            if (offset != null && (offset < 0 || offset > 100000)) // max specification 10'000
            {
                return BadRequest("OGC API if set offset has to be between 1 and 100'000");
            }

            var bboxDouble = new List<double>();

            if (!string.IsNullOrEmpty(bbox))
            {
                try
                {
                    bboxDouble = bbox.Split(',').Select(Convert.ToDouble).ToList();
                }
                catch (Exception)
                {
                    return BadRequest("OGC API bbox are no valid numbers");
                }

                if ((bboxDouble.Count < 4 || bboxDouble.Count > 6))
                {
                    return BadRequest("OGC API bbox has to be an array of double values, 4-6 items");
                }

                if (bboxDouble.Count == 4 && !(bboxDouble[2] > bboxDouble[0] && bboxDouble[3] > bboxDouble[1]))
                {
                    return BadRequest("OGC API bbox has to be xmin,ymin,xmax,ymax");
                }

                if (bboxDouble.Count == 6 && !(bboxDouble[3] > bboxDouble[0] && bboxDouble[4] > bboxDouble[1] && bboxDouble[5] > bboxDouble[2]))
                {
                    return BadRequest("OGC API bbox has to be xmin,ymin,zmin,xmax,ymax,zmax");
                }
            }

            // todo checks for filter

            var featureCollection = OgcApiFeaturesService.GetItems(Config, project, collectionId, bboxDouble, bboxCrs, query, offset,limit, f);

            var content = JsonConvert.SerializeObject(featureCollection.Value);

            if (file)
            {
                return new FileContentResult(Encoding.UTF8.GetBytes(content), "application/geo+json") { FileDownloadName = $"{Guid.NewGuid()}.geojson" };
            }

            Response.Headers.Append("Content-Crs", featureCollection.Value.Crs);
            
            return new FileContentResult(Encoding.UTF8.GetBytes(content), "application/geo+json");
        }
    }
}
