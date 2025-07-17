using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.OgcApi.Features;
using BMapr.GDAL.WebApi.Models.Spatial;
using BMapr.GDAL.WebApi.Services;
using BMapr.GDAL.WebApi.Services.OgcApi;
using BMapr.WebApi.Controllers;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BMapr.GDAL.WebApi.Controllers
{
    [ApiController]
    [Route("api/oaf/features")]
    public class OafController : DefaultController
    {
        private readonly ILogger<OgcController> _logger;
        private static List<CrsDefinition> CrsList = new();

        public OafController(ILogger<OgcController> logger, IConfiguration iConfig, IWebHostEnvironment environment, IMemoryCache cache)
            : base(iConfig, environment)
        {
            _logger = logger;
            if (CrsList.Count == 0)
            {
                CrsList = CrsService.GetData(logger, iConfig, environment);
            }
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

                if (bboxDouble.Count < 4 || bboxDouble.Count > 6)
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
            var protectedParameters = new List<string>
                { "project", "collectionId", "bbox", "bbox-crs", "crs", "offset", "limit", "f", "file", "filter-lang", "filter" };
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

            _logger.LogInformation(
                $"project {project}, collectionId {collectionId}, bbox: {string.Join(',', bboxDouble.Select(x => x.ToString()))}, query: {query}, offset {offset}, limit {limit}, f: {f}, filter-lang: {filterLang}, filter: {filter}");

            var url = Request.GetDisplayUrl();

            var oafService = new OgcApiFeatureMsSql(Config, project, CrsList);
            var request = new GetItemRequest()
            {
                ConnectionParameters = new Dictionary<string, object>()
                {
                    { OgcApiFeatureMsSql.Connectionstring,"Server=localhost\\\\SQLEXPRESS,1433;uid=sa;pwd=sa;database=MapserverTest;TrustServerCertificate=True"},
                    { OgcApiFeatureMsSql.DataTable,"bb_ori"},
                    { OgcApiFeatureMsSql.DataWhere,""},
                    { OgcApiFeatureMsSql.DataIdField,"qgs_fid"},
                    { OgcApiFeatureMsSql.DataGeometryField,"geom"},
                    { OgcApiFeatureMsSql.DataGeometryEpsgCode,2056},
                    { OgcApiFeatureMsSql.DataGeometrySrid,2056},
                    { OgcApiFeatureMsSql.DataUseGeography,false},
                },
                CollectionId = "bb",
                Bbox = bboxDouble,
                Crs = crs,
                Offset = offset,
                Limit = limit,
                Format = f,
                Host = Request.Host.Host

            };

            var result = oafService.GetItems(request);

            //var result = OgcApiFeaturesService.GetItems(Config, project, collectionId, bboxDouble, bboxCrs, crs, query, filter, offset, limit, f, url, Request.Host.Host);

            // todo add messages and exceptions from result to log

            if (file)
            {
                result.Value.FileDownloadName = $"{Guid.NewGuid()}.geojson";
            }

            //todo reactivate
            //Response.Headers.Append("Content-Crs", featureCollection.Value.Crs);

            return result.Value;
        }
    }
}
