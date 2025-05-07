using BMapr.GDAL.WebApi.Models.OgcApi.Features;
using BMapr.GDAL.WebApi.Models.Spatial;
using BMapr.GDAL.WebApi.Services;
using BMapr.WebApi.Controllers;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text;

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
        private static List<CrsDefinition> CrsList = new();

        public OgcApiFeatuesController(ILogger<OgcController> logger, IConfiguration iConfig, IWebHostEnvironment environment, IMemoryCache cache) : base(iConfig, environment)
        {
            _logger = logger;
            if (CrsList.Count == 0)
            {
                CrsList = CrsService.GetData(logger, iConfig, environment);
            }
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
        public ActionResult LandingPage(string project, [FromQuery] string? service, [FromQuery] string f = "application/json")
        {
            if (!string.IsNullOrEmpty(service) && service.ToLower() == "wfs")
            {
                return new StatusCodeResult(400);
            }

            // no alternate format supported

            if (f.ToLower() != "application/json" && f.ToLower() != "json")
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

            if (f.ToLower() != "application/json" && f.ToLower() != "json")
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
                    "http://www.opengis.net/spec/ogcapi-common-1/1.0/conf/core",
                    "http://www.opengis.net/spec/ogcapi-common-1/1.0/conf/json",
                    "http://www.opengis.net/spec/ogcapi-common-1/1.0/conf/landing-page",
                    "http://www.opengis.net/spec/ogcapi-common-2/1.0/conf/collections",
                    "http://www.opengis.net/spec/ogcapi-features-1/1.0/conf/core",
                    "http://www.opengis.net/spec/ogcapi-features-1/1.0/conf/geojson",
                    "http://www.opengis.net/spec/ogcapi-features-2/1.0/conf/crs",
                    "http://www.opengis.net/spec/ogcapi-features-3/1.0/conf/queryables",
                    "http://www.opengis.net/spec/ogcapi-features-5/1.0/conf/schemas",
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

            if (f.ToLower() != "application/json" && f.ToLower() != "json")
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

            var result = OgcApiFeaturesService.GetCollections(Config, CrsList, project, collections, urlCollections);
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

            if (f.ToLower() != "application/json" && f.ToLower() != "json")
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
            var result = OgcApiFeaturesService.GetCollection(Config, CrsList, project, collectionId, urlCollections);
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
        /// <param name="crs">CRS of the response</param>
        /// <param name="offset">paging start offset to skip</param>
        /// <param name="limit">page size</param>
        /// <param name="f">format of the queried data</param>
        /// <param name="file">download data as file</param>
        /// <returns></returns>
        [HttpGet("{project}/collections/{collectionId}/items")]
        [HttpHead("{project}/collections/{collectionId}/items")]
        [HttpOptions("{project}/collections/{collectionId}/items")]
        public ActionResult Feature(
            string project, 
            string collectionId, 
            [FromQuery] string? bbox, 
            [FromQuery(Name = "bbox-crs")] string? bboxCrs, 
            [FromQuery] string? crs, 
            [FromQuery] int? offset, 
            [FromQuery] int? limit,
            [FromQuery(Name = "filter-lang")] string? filterLang,
            [FromQuery] string? filter,
            [FromQuery] string f = "geojson", 
            [FromQuery] bool file = false
        )
        {
            if (f.ToLower() != "geojson" && f.ToLower() != "json")
            {
                return BadRequest($"OGC API format <<{f}>> not supported");
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

            if (!string.IsNullOrEmpty(crs))
            {
                if (!(crs.ToLower().StartsWith("http://www.opengis.net/def/crs/epsg/0/") || crs.ToLower() == "http://www.opengis.net/def/crs/ogc/1.3/crs84"))
                {
                    return BadRequest("OGC API feature, bad CRS definition");
                }
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

            if (!string.IsNullOrEmpty(filterLang) && filterLang.ToLower() != "cql-text")
            {
                return BadRequest("OGC API filter-lang has to be cql-text");
            }

            if (!string.IsNullOrEmpty(filterLang) && filterLang.ToLower() == "cql-text" && string.IsNullOrEmpty(filter?.Trim()))
            {
                return BadRequest("OGC API filter needs a value because filter-lang is set");
            }

            var queryParameter = Request.Query.ToDictionary(x => x.Key, y => y.Value.ToString());
            var protectedParameters = new List<string>()
                {"project", "collectionId", "bbox", "bbox-crs", "crs", "offset", "limit", "f", "file","filter-lang","filter"};
            var query = string.Empty;
            var index = 0;

            foreach (var keyValuePair in queryParameter)
            {
                if (protectedParameters.Contains(keyValuePair.Key.ToLower()))
                {
                    continue;
                }

                if (index > 0)
                {
                    query += $"{query} AND ";
                }

                query = $"{keyValuePair.Key}={keyValuePair.Value}";
                index++;
            }

            if (index > 0 && !string.IsNullOrEmpty(filter?.Trim()))
            {
                return BadRequest("OGC API to set filter and query don't make sense");
            }

            _logger.LogInformation($"project {project}, collectionId {collectionId}, bbox: {string.Join(',',bboxDouble.Select(x => x.ToString()))}, query: {query}, offset {offset}, limit {limit}, f: {f}, filter-lang: {filterLang}, filter: {filter}");

            var url = Request.GetDisplayUrl();

            var featureCollection = OgcApiFeaturesService.GetItems(Config, project, collectionId, bboxDouble, bboxCrs, crs, query, filter, offset,limit, f);

            // todo add messages and exceptions from result to log

            var content = JsonConvert.SerializeObject(featureCollection.Value);

            if (file)
            {
                return new FileContentResult(Encoding.UTF8.GetBytes(content), "application/geo+json") { FileDownloadName = $"{Guid.NewGuid()}.geojson" };
            }

            Response.Headers.Append("Content-Crs", featureCollection.Value.Crs);
            
            return new FileContentResult(Encoding.UTF8.GetBytes(content), "application/geo+json");
        }

        [HttpGet("{project}/collections/{collectionId}/queryables")]
        [HttpHead("{project}/collections/{collectionId}/queryables")]
        [HttpOptions("{project}/collections/{collectionId}/queryables")]
        public ActionResult Queryables(string project, string collectionId, [FromQuery] string f = "json")
        {
            if (f.ToLower() != "json")
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

            var queryables = OgcApiFeaturesService.GetQueryables(Config, project, collectionId, f);

            var content = JsonConvert.SerializeObject(queryables.Value, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            return new FileContentResult(Encoding.UTF8.GetBytes(content), "application/schema+json");
        }
    }
}
