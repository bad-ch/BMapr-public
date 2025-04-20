using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.Spatial;
using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Diagnostics;
using System.Web;

namespace BMapr.GDAL.WebApi.Controllers
{
    /// <summary>
    /// More lightweight GIS data interface than OGC
    /// </summary>
    [ApiController]
    [Route("api/Spatial")]
    public class SpatialController : DefaultController
    {
        private readonly ILogger<SpatialController> _logger;

        public SpatialController(ILogger<SpatialController> logger, IConfiguration iConfig, IWebHostEnvironment environment)
            : base(iConfig, environment)
        {
            _logger = logger;
        }

        /// <summary>
        /// Send XML or JSON structure with new created, modified or to delete features
        /// </summary>
        /// <returns>XML or JSON response with the status and result of the action</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost("Crud")]
        public ActionResult Crud()
        {
            throw new NotImplementedException("work in progress, priority 1");
        }

        /// <summary>
        /// Get file based GIS data (geojson, kml, ...)
        /// </summary>
        /// <returns>Deliver GIS data</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("{project}/{file}")]
        public ActionResult GetFile(string project, string file, [FromQuery(Name = "token")] string? token)
        {
            // todo handling from subfolders, idea replace | with /

            var projectSettings = ProjectSettingsService.Get(project, Config);

            if (!TokenService.Check(Request, IConfig, projectSettings, token))
            {
                return BadRequest("User or system token invalid");
            }

            var dataPath = Config.DataProject(project);
            var filePath = Path.Combine(dataPath.FullName, file);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"file {file} not found");
            }

            var mimeType = FileService.GetMimeType(filePath);
            var extension = FileService.GetExtension(filePath);

            if (mimeType == null)
            {
                return BadRequest($"file {file} with this extension not supported");
            }

            var urlFile = filePath.Replace(extension, $"{extension}.url");

            if (System.IO.File.Exists(urlFile))
            {
                try
                {
                    var urlConfig = System.IO.File.ReadAllText(urlFile);
                    var webRequestParameter = JsonConvert.DeserializeObject<WebRequestParameter>(urlConfig);
                    var remoteContent = WebRequestService.Request(webRequestParameter);

                    if (remoteContent.Succesfully)
                    {
                        //todo check mime type is the same
                        System.IO.File.WriteAllBytes(filePath, remoteContent.Value);
                    }
                    else
                    {
                        _logger.LogWarning($"error renew file {file}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"error renew file {file}, {ex.Message}");
                }
            }

            return new FileContentResult(System.IO.File.ReadAllBytes(filePath), mimeType);
        }

        /// <summary>
        /// Get file as partial stream
        /// </summary>
        /// <returns>Deliver Protomap tiles data</returns>
        [HttpGet("{project}/{file}/pmtiles")]
        public ActionResult GetPmTile(string project, string file)
        {
            var dataPath = Config.DataProject(project);
            var filePath = Path.Combine(dataPath.FullName, HttpUtility.UrlDecode(file));

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"file {file} not found");
            }

            var mimeType = FileService.GetMimeType(filePath);
            var extension = FileService.GetExtension(filePath);

            if (mimeType == null || extension != ".pmtiles")
            {
                return BadRequest($"only pm tiles are supported");
            }

            return File(new FileStream(filePath,FileMode.Open, FileAccess.Read, FileShare.Read), "application/octet-stream", true);
        }

        /// <summary>
        /// Get file based GIS data (geojson, kml, ...) and reproject on the fly
        /// </summary>
        /// <param name="project">Name project</param>
        /// <param name="file">Filename of the project</param>
        /// <param name="epsgSource">Source epsg code as integer</param>
        /// <param name="epsgTarget">Target epsg code as integer</param>
        /// <param name="formatTarget">Extension of the wished format, 'none' means no conversion</param>
        /// <param name="token">Access token project</param>
        /// <returns>Converted and/or Reprojected GIS data</returns>
        [HttpGet("Convert/{project}/{file}/{epsgSource}/{epsgTarget}/{formatTarget}")]
        public ActionResult Reproject(string project, string file, int epsgSource, int epsgTarget, string formatTarget, [FromQuery(Name = "token")] string? token)
        {
            var projectSettings = ProjectSettingsService.Get(project, Config);

            if (!TokenService.Check(Request, IConfig, projectSettings, token))
            {
                return BadRequest("User or system token invalid");
            }

            var dataPath = Config.DataProject(project);
            var filePath = Path.Combine(dataPath.FullName, HttpUtility.UrlDecode(file));

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"file {file} not found");
            }

            var extension = formatTarget.ToLower() == "none" ? new FileInfo(filePath).Extension : $".{formatTarget}";
            var filePathTarget = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");

            var status = GisDataService.Convert(filePath, filePathTarget, epsgSource, epsgTarget);

            if (!status)
            {
                return BadRequest($"File conversion/ reprojection {file} to {formatTarget} from EPSG:{epsgSource} to EPSG:{epsgTarget} FAIL");
            }

            //todo add an intelligent cache mechanism

            var mimeType = FileService.GetMimeType(filePath);

            return new FileContentResult(System.IO.File.ReadAllBytes(filePathTarget), new MediaTypeHeaderValue(mimeType));
        }

        /// <summary>
        /// Get file based GIS data (geojson, kml, ...) and convert and reproject on the fly
        /// </summary>
        /// <returns>Reprojected and converted GIS data</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("Convert/{format}/{epsg}")]
        public ActionResult Convert(string format, int epsg)
        {
            throw new NotImplementedException("work in progress, priority 1");
        }

        /// <summary>
        /// Get file based GIS data (geojson, kml, ...) and convert, filter and reproject on the fly
        /// </summary>
        /// <returns>Reprojected, converted and filtered GIS data</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost("Convert")]
        public ActionResult ConvertPost()
        {
            // In the body we have information about the format, epsg and some filtering (javascript expression for doing something)
            throw new NotImplementedException("work in progress, priority 2");
        }

        /// <summary>
        /// Process file based GIS data (geojson, kml, ...) with different actions, spatial operations ...
        /// </summary>
        /// <returns>Processed GIS data</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost("Process")]
        public ActionResult ProcessPost()
        {
            // In the body we have information about the format, epsg and some filtering (javascript expression for doing something)
            throw new NotImplementedException("work in progress, priority 3");
        }

        /// <summary>
        /// Get metadata from file based GIS data (geojson, kml, ...)
        /// </summary>
        /// <returns>GIS Metadata</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("Information/{project}/{file}")]
        public ActionResult GetInformation(string project, string file)
        {
            var result = new List<string>();

            var gdalPath = Path.Combine(Config.AssemblyPath?.FullName, "gdal/x64");
            System.Environment.SetEnvironmentVariable("PATH", gdalPath, EnvironmentVariableTarget.Process);

            using (var process = new Process())
            {
                var appPath = Path.Combine(Config.AssemblyPath?.FullName, "gdal/x64/apps");
                var projectPath = Path.Combine(Config.DataProjects?.FullName, project);
                var filepath = Path.Combine(projectPath, file);

                process.StartInfo.FileName = Path.Combine(appPath, "ogrinfo.exe");
                process.StartInfo.Arguments = $"-ro -al -so -json {filepath}"; // -json in GDAL 3.7
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;


                process.OutputDataReceived += (sender, data) => result.Add(data.Data);
                process.ErrorDataReceived += (sender, data) => result.Add(data.Data);
                Console.WriteLine("starting");
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine(); // (optional) wait up to 10 seconds  
                do
                {
                    if (!process.HasExited)
                    {
                        // Refresh the current process property values.  
                        process.Refresh();
                        Console.WriteLine($"exit {process.HasExited}");
                    }
                }
                while (!process.WaitForExit(1000));
            }

            return new ContentResult() { Content = string.Join(string.Empty, result), ContentType = "text/plain", StatusCode = 200 }; //ContentType = "application/json"
        }

        /// <summary>
        /// Get proj4js defintions and some meta data from crs
        /// </summary>
        /// <param name="epsg"></param>
        /// <returns></returns>
        [HttpGet("GetProj4Js/{epsg}")]
        public ActionResult GetProj4Js(int epsg)
        {
            var dataSharePath = Config.DataShare;
            var pathCrsFile = Path.Combine(dataSharePath.FullName, $"CrsDefinitionList.json");

            if (!System.IO.File.Exists(pathCrsFile))
            {
                _logger.LogError($"crs file not found: {pathCrsFile}");
                return NotFound("crs file not found");
            }

            var crsDefinitionsString = System.IO.File.ReadAllText(pathCrsFile);
            CrsDefinition? crsDefinitionFound;

            try
            {
                var crsDefinitions = JsonConvert.DeserializeObject<List<CrsDefinition>>(crsDefinitionsString);
                if (crsDefinitions == null || !crsDefinitions.Any())
                {
                    return NotFound("no crs definitions found");
                }

                crsDefinitionFound = crsDefinitions.FirstOrDefault(x => x.Epsg == epsg);
                if (crsDefinitionFound == null)
                {
                    return BadRequest($"projection <{epsg}> not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"DES error: {pathCrsFile}, {ex.Message}");
                return StatusCode(500, "crs definitions DES error");
            }

            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            };

            var content = JsonConvert.SerializeObject(
                crsDefinitionFound,
                Formatting.None,
                new JsonSerializerSettings()
                {
                    ContractResolver = contractResolver,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                }
            );

            return new ContentResult() { Content = content, ContentType = "application/json", StatusCode = 200 };
        }
    }
}
