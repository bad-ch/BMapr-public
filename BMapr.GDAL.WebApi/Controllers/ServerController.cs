using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;

namespace BMapr.GDAL.WebApi.Controllers
{
    /// <summary>
    /// Server specific endpoints
    /// </summary>
    [ApiController]
    [Route("api/Server")]
    public class ServerController : DefaultController
    {
        private readonly ILogger<ServerController> _logger;

        public ServerController(ILogger<ServerController> logger, IConfiguration iConfig, IWebHostEnvironment environment) : base(iConfig, environment)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get version of server, GDAL, OGR and Mapserver, incl. supported drivers
        /// </summary>
        /// <returns>JSON with content</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("Version")]
        [HttpHead("Version")]
        public ActionResult Version([FromQuery(Name = "token")] string token)
        {
            if (!TokenService.Check(Request, IConfig, null, token))
            {
                return BadRequest("System token invalid, user token not allowed");
            }

            //GdalConfiguration.ConfigureGdal();
            //GdalConfiguration.ConfigureOgr();

            var versionFilePath = Path.Combine(Config.AssemblyPath.FullName, "version.json");
            var versionData = new VersionData();

            if (System.IO.File.Exists(versionFilePath))
            {
                var versionContent = System.IO.File.ReadAllText(versionFilePath);
                if (!string.IsNullOrEmpty(versionContent))
                {
                    versionData = JsonConvert.DeserializeObject<VersionData>(versionContent);
                }
            }

            var os = System.Environment.OSVersion;
            var ogrDrivers = GdalConfiguration.GetDriversOgr();
            var gdalDrivers = GdalConfiguration.GetDriversGdal();

            return new ContentResult()
            {
                Content = JsonConvert.SerializeObject(new
                {
                    Server = new
                    {
                        Product = "BMapr",
                        versionData?.Version,
                        Stack = $"{Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName}, Windows version {os.Platform}, {os.VersionString}",
                        versionData?.Branch,
                        versionData?.Tag,
                        versionData?.Commit,
                        Culture = CultureInfo.CurrentCulture.Name,
                        versionData?.CreationDate,
                        versionData?.CreationTime,
                        Path = new {
                            Temp = Path.GetTempPath(),
                            Data = Config.Data?.FullName,
                            Assembly = Config.AssemblyPath?.FullName,
                            ApplicationRoot = Config.ApplicationRoot?.FullName,
                            Projects = Config.DataProjects?.FullName,
                        },
                        Environment = GdalConfiguration.GdalPaths,
                        WebApplication = Request.PathBase,
                    },
                    Gdal = new
                    {
                        Version = GdalConfiguration.GetVersionGdal(),
                        gdalDrivers.Count,
                        Drivers = gdalDrivers.OrderBy(x => x)
                    },
                    Ogr = new
                    {
                        Version = GdalConfiguration.GetVersionOgr(),
                        ogrDrivers.Count,
                        Drivers = ogrDrivers.OrderBy(x => x)
                    },
                    Mapserver = new
                    {
                        Version = GdalConfiguration.GetVersionMapserver(),
                        Options = GdalConfiguration.GetCompiledOptionsMapserver()
                    },
                }, Formatting.Indented),
                ContentType = "application/json",
                StatusCode = 200
            };
        }

        /// <summary>
        /// Ping of server, GDAL, OGR and Mapserver, incl. supported drivers
        /// </summary>
        /// <returns>JSON with content</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("Ping")]
        [HttpHead("Ping")]
        public ActionResult Ping()
        {
            //GdalConfiguration.ConfigureGdal();
            //GdalConfiguration.ConfigureOgr();

            var versionFilePath = Path.Combine(Config.AssemblyPath.FullName, "version.json");
            var versionData = new VersionData();

            if (System.IO.File.Exists(versionFilePath))
            {
                var versionContent = System.IO.File.ReadAllText(versionFilePath);
                if (!string.IsNullOrEmpty(versionContent))
                {
                    versionData = JsonConvert.DeserializeObject<VersionData>(versionContent);
                }
            }

            var os = System.Environment.OSVersion;
            var ogrDrivers = GdalConfiguration.GetDriversOgr();
            var gdalDrivers = GdalConfiguration.GetDriversGdal();

            return new ContentResult()
            {
                Content = JsonConvert.SerializeObject(new
                {
                    Server = new
                    {
                        Product = "BMapr",
                        versionData?.Version,
                        Stack = $"{Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName}, Windows version {os.Platform}, {os.VersionString}",
                        versionData?.Branch,
                        versionData?.Tag,
                        versionData?.Commit,
                        Culture = CultureInfo.CurrentCulture.Name,
                        versionData?.CreationDate,
                        versionData?.CreationTime,
                        Guid = Guid.NewGuid(),
                        RequestTime = DateTime.Now.ToUniversalTime(),
                    },
                    Gdal = new
                    {
                        Version = GdalConfiguration.GetVersionGdal(),
                        gdalDrivers.Count,
                    },
                    Ogr = new
                    {
                        Version = GdalConfiguration.GetVersionOgr(),
                        ogrDrivers.Count,
                    },
                    Mapserver = new
                    {
                        Version = GdalConfiguration.GetVersionMapserver(),
                    },
                }, Formatting.Indented),
                ContentType = "application/json",
                StatusCode = 200
            };
        }
    }
}
