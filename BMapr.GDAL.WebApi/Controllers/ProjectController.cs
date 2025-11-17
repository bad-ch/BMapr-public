using System.Drawing.Imaging;
using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BMapr.GDAL.WebApi.Controllers
{
    /// <summary>
    /// Project interface, create, update, delete MapServer mapfiles
    /// </summary>
    [ApiController]
    [Route("api/Project")]
    public class ProjectController : DefaultController
    {
        private readonly ILogger<ProjectController> _logger;
        private readonly IMemoryCache _cache;

        public ProjectController(ILogger<ProjectController> logger, IConfiguration iConfig, IWebHostEnvironment environment, IMemoryCache cache) : base(iConfig, environment)
        {
            _logger = logger;
            _cache = cache;

            if (_cache.Get("mapContent") == null)
            {
                _cache.Set("mapContent", new Dictionary<string, string>(), TimeSpan.FromDays(1));
            }
            if (_cache.Get("mapConfig") == null)
            {
                _cache.Set("mapConfig", new Dictionary<string, MapConfig>(), TimeSpan.FromDays(1));
            }
        }

        /// <summary>
        /// Receive html form for uploading MapServer mapfiles and data
        /// </summary>
        /// <param name="token">Access token</param>
        /// <returns>Html page</returns>
        [HttpGet("CreationForm")]
        public ActionResult CreationForm([FromQuery(Name = "token")] string? token)
        {
            if (!TokenService.Check(Request, IConfig, null, token))
            {
                return BadRequest("User or system token invalid");
            }

            var htmlTemplatePath = Path.Combine(Config.Data.FullName, "config/getForm.html");
            var content = System.IO.File.ReadAllText(htmlTemplatePath);

            content = content.Replace("#action#", "./Create");
            content = content.Replace("#method#", "Post");
            content = content.Replace("#titleForm#", "Create new project");
            content = content.Replace("#titleHead#", "BMapr GDAL OGC server, creation form");

            return new ContentResult()
            {
                Content = content.Trim(),
                ContentType = "text/html",
                StatusCode = 200
            };
        }

        /// <summary>
        /// Receive html form for update MapServer mapfiles and data
        /// </summary>
        /// <param name="project">Guid from project</param>
        /// <param name="token">Access token</param>
        /// <returns>Html page</returns>
        [HttpGet("UpdateForm/{project}")]
        public ActionResult UpdateForm(string project, [FromQuery(Name = "token")] string? token)
        {
            var projectSettings = ProjectSettingsService.Get(project, Config);

            if (!TokenService.Check(Request, IConfig, projectSettings, token))
            {
                return BadRequest("User or system token invalid");
            }

            var htmlTemplatePath = Path.Combine(Config.Data.FullName, "config/getForm.html");
            var content = System.IO.File.ReadAllText(htmlTemplatePath);

            content = content.Replace("#action#", $"../Update/{project}");
            content = content.Replace("#method#", "Post");
            content = content.Replace("#titleForm#", "Update project");
            content = content.Replace("#titleHead#", "BMapr GDAL OGC server, update form");

            return new ContentResult()
            {
                Content = content.Trim(),
                ContentType = "text/html",
                StatusCode = 200
            };
        }

        /// <summary>
        /// Create project with a zip file upload, directly without any form
        /// </summary>
        /// <returns>Json with the guid from the project</returns>
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)] //todo
        [HttpPost("Create"), DisableRequestSizeLimit]
        public ActionResult Create()
        {
            //todo security;

            var dataProjects = Config.DataProjects?.FullName;
            var guid = Guid.NewGuid();
            var dataProject = Path.Combine(dataProjects, guid.ToString());

            Directory.CreateDirectory(dataProject);
            var projectSettings = ProjectSettingsService.CreateNew(guid.ToString(), Config);

            var result = FileService.CopyDataToProjectFolder(Request, dataProject);

            if (!result.Succesfully)
            {
                return BadRequest("Create project fail");
            }

            FileService.UpdateMapFile(dataProject, guid.ToString(), Config, HostService.Get(Request, IConfig));

            return new JsonResult(new
            {
                projectGuid = guid, 
                projectSettings = projectSettings, 
                allowedFiles = result.Value.AllowedFiles, 
                forbiddenFiles= result.Value.ForbiddenFiles, 
                messages = result.Messages
            }) 
            { ContentType = "application/json", StatusCode = 200};
        }

        /// <summary>
        /// Upload project with a zip file upload, directly without any form
        /// </summary>
        /// <returns>Json with the guid from the project</returns>
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)] //todo
        [HttpPost("Update/{project}"), DisableRequestSizeLimit]
        public ActionResult Update(string project)
        {
            //todo security;

            var dataProjects = Config.DataProjects?.FullName;
            var dataProject = Path.Combine(dataProjects, project);
            var projectSettings = ProjectSettingsService.Get(project, Config);

            var status = FileService.CleanupProjectFolder(dataProject);

            if (!status.Succesfully)
            {
                _logger.LogError(JsonConvert.SerializeObject(status));
                return BadRequest("Update project, cleanup project fail");
            }

            var result = FileService.CopyDataToProjectFolder(Request, dataProject);

            if (!result.Succesfully)
            {
                _logger.LogError(JsonConvert.SerializeObject(result));
                return BadRequest("Update project, copy new project fail");
            }

            FileService.UpdateMapFile(dataProject, project, Config, HostService.Get(Request, IConfig));

            return new JsonResult(new
                {
                    projectGuid = project,
                    projectSettings = projectSettings,
                    allowedFiles = result.Value.AllowedFiles,
                    forbiddenFiles = result.Value.ForbiddenFiles,
                    messages = result.Messages
                })
                { ContentType = "application/json", StatusCode = 200 };
        }

        /// <summary>
        /// Get project as a zip file
        /// </summary>
        /// <param name="project">Guid from project</param>
        /// <param name="token">Access token</param>
        /// <returns>Zipped file with mapfile and data</returns>
        [HttpGet("Get/{project}")]
        public ActionResult Get(string project, [FromQuery(Name = "token")] string? token)
        {
            var dataProjects = Config.DataProjects?.FullName;
            var dataProject = Path.Combine(dataProjects, project);

            if (!Directory.Exists(dataProject))
            {
                return NotFound("Project not found");
            }

            var projectSettings = ProjectSettingsService.Get(project, Config);

            if (!TokenService.Check(Request, IConfig, projectSettings,token))
            {
                return BadRequest("User or system token invalid");
            }

            var byteContent = ZipService.Archive(dataProject);

            return new FileContentResult(byteContent, "application/zip");
        }

        /// <summary>
        /// Get file from project
        /// </summary>
        /// <param name="project">Guid from project</param>
        /// <param name="token">Access token</param>
        /// <param name="filePath"></param>
        /// <param name="attachment">true return as attachment otherwise inline</param>
        /// <returns>return single file</returns>
        [HttpGet("GetFile/{project}")]
        public ActionResult GetFile(string project, [FromQuery(Name = "token")] string token, [FromQuery(Name = "filePath")] string filePath, [FromQuery(Name = "attachment")] bool attachment = false)
        {
            var dataProjects = Config.DataProjects?.FullName;
            var dataProject = Path.Combine(dataProjects, project);

            if (!Directory.Exists(dataProject))
            {
                return NotFound("Project not found");
            }

            var projectSettings = ProjectSettingsService.Get(project, Config);

            if (!TokenService.Check(Request, IConfig, projectSettings, token))
            {
                return BadRequest("User or system token invalid");
            }

            var absFilePath = Path.Combine(dataProject, filePath);

            if (!System.IO.File.Exists(absFilePath))
            {
                return NotFound();
            }
            
            var content = System.IO.File.ReadAllBytes(absFilePath);

            if (content.Length > 20000000)
            {
                return BadRequest("file is bigger than 20 MB");
            }

            var fileInfo = new FileInfo(absFilePath);
            var mimeType = MimeTypeService.Get(fileInfo.Extension);

            if (!attachment)
            {
                Response.Headers.Append("Content-Disposition", $"inline; filename={fileInfo.Name}");
            }
            else
            {
                Response.Headers.Append("Content-Disposition", $"attachment; filename={fileInfo.Name}");
            }

            return new FileContentResult(content, mimeType);
        }

        /// <summary>
        /// Get mapfile as JSON file
        /// </summary>
        /// <param name="project">Guid from project</param>
        /// <param name="token">Access token</param>
        /// <returns>Mapfile as JSON</returns>
        [HttpGet("GetMapData/{project}")]
        public ActionResult GetMapData(string project, [FromQuery(Name = "token")] string? token)
        {
            var projectSettings = ProjectSettingsService.Get(project, Config);

            if (!TokenService.Check(Request, IConfig, projectSettings, token))
            {
                return BadRequest("User or system token invalid");
            }

            Config.Host = HostService.Get(Request, IConfig);

            var mapserverService = new MapserverService(Config, project);
            var result = mapserverService.GetMetadata(mapserverService.Map);

            result["projectGuid"] = project;

            var content = JsonConvert.SerializeObject(
                result, 
                Formatting.None,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }
            );

            return new ContentResult()
            {
                Content = content,
                ContentType = "application/json",
                StatusCode = 200
            };
        }

        /// <summary>
        /// Get legend with encoded images from map
        /// </summary>
        /// <param name="project">Guid from project</param>
        /// <param name="token">Access token</param>
        /// <returns>Legend from map file as JSON</returns>
        [HttpGet("GetLegend/{project}")]
        public ActionResult GetLegend(string project, [FromQuery(Name = "token")] string? token)
        {
            var projectSettings = ProjectSettingsService.Get(project, Config);

            if (!TokenService.Check(Request, IConfig, projectSettings, token))
            {
                return BadRequest("User or system token invalid");
            }

            Config.Host = HostService.Get(Request, IConfig);

            var mapMetadata = MapFileService.GetMapFromProject(project, Config);

            if (!mapMetadata.Succesfully)
            {
                BadRequest("Map not found");
            }

            var mapPath = MapFileService.GetMapPathFromCache( mapMetadata.Value.Key, mapMetadata.Value.FilePath, Config, project) ?? string.Empty;
            var mapServer = new Mapserver(new FileInfo(mapPath));
            var legendItems = mapServer.GetLegendAsImages(180, 90, true);
            var legends = legendItems.Select( x => new {label = x.Key, content = ImageService.GetBase64StringFromImage(x.Value, ImageFormat.Png)});

            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            var content = JsonConvert.SerializeObject(
                legends,
                Formatting.None,
                new JsonSerializerSettings()
                {
                    ContractResolver = contractResolver,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }
            );

            return new ContentResult()
            {
                Content = content,
                ContentType = "application/json",
                StatusCode = 200
            };
        }

        /// <summary>
        /// Get legend as a list of sld's from the map
        /// </summary>
        /// <param name="project">Guid from project</param>
        /// <param name="token">Access token</param>
        /// <returns>List of sld's from map file as JSON</returns>
        [HttpGet("GetLegendAsSld/{project}")]
        public ActionResult GetLegendAsSld(string project, [FromQuery(Name = "token")] string? token)
        {
            var projectSettings = ProjectSettingsService.Get(project, Config);

            if (!TokenService.Check(Request, IConfig, projectSettings, token))
            {
                return BadRequest("User or system token invalid");
            }

            Config.Host = HostService.Get(Request, IConfig);

            var mapMetadata = MapFileService.GetMapFromProject(project, Config);

            if (!mapMetadata.Succesfully)
            {
                BadRequest("Map not found");
            }

            var mapPath = MapFileService.GetMapPathFromCache(mapMetadata.Value.Key, mapMetadata.Value.FilePath, Config, project) ?? string.Empty;
            var mapServer = new Mapserver(new FileInfo(mapPath));
            var legends = mapServer.GetLegendAsSld(true);

            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            var content = JsonConvert.SerializeObject(
                legends,
                Formatting.None,
                new JsonSerializerSettings()
                {
                    ContractResolver = contractResolver,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }
            );

            return new ContentResult()
            {
                Content = content,
                ContentType = "application/json",
                StatusCode = 200
            };
        }

        /// <summary>
        /// Delete project
        /// </summary>
        /// <param name="project">Guid from project</param>
        /// <param name="token">Access token</param>
        /// <returns></returns>
        [HttpDelete("Delete/{project}")]
        public ActionResult Delete(string project, [FromQuery(Name = "token")] string? token)
        {
            var dataProjects = Config.DataProjects?.FullName;
            var dataProject = Path.Combine(dataProjects, project);

            if (!Directory.Exists(dataProject))
            {
                return NotFound("Project not found");
            }

            var projectSettings = ProjectSettingsService.Get(project, Config);

            if (!TokenService.Check(Request, IConfig, projectSettings, token))
            {
                return BadRequest("User or system token invalid");
            }

            try
            {
                Directory.Delete(dataProject, true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"error delete project {project}, {ex.Message}");
                return BadRequest($"error delete project {project}");
            }

            return Ok($"Delete project {project} successfully");
        }

        /// <summary>
        /// Get log file from MapServer, if available
        /// </summary>
        /// <param name="project">Guid from project</param>
        /// <param name="token">Access token</param>
        /// <returns>Text file with the log content</returns>
        [HttpGet("GetMapserverLog/{project}")]
        public ActionResult GetMapserverLog(string project, [FromQuery(Name = "token")] string? token)
        {
            var dataProjects = Config.DataProjects?.FullName;
            var dataProject = Path.Combine(dataProjects, project);

            if (!Directory.Exists(dataProject))
            {
                return NotFound("Project not found");
            }

            var projectSettings = ProjectSettingsService.Get(project, Config);

            if (!TokenService.Check(Request, IConfig, projectSettings, token))
            {
                return BadRequest("User or system token invalid");
            }

            var firstLogFile = new DirectoryInfo(dataProject).GetFiles("*.log").FirstOrDefault();

            if (firstLogFile == null || !System.IO.File.Exists(firstLogFile.FullName))
            {
                return NotFound("No log file found");
            }

            var byteContent = System.IO.File.ReadAllBytes(firstLogFile.FullName);

            return new FileContentResult(byteContent, "text/plain");
        }

        /// <summary>
        /// Show the project data as a map (Openlayers)
        /// </summary>
        /// <param name="project">Guid from project</param>
        /// <param name="token">Access token</param>
        /// <returns>Text file with the log content</returns>
        [HttpGet("OpenLayers/{project}")]
        public ActionResult OpenLayers(string project, [FromQuery(Name = "token")] string? token)
        {
            var dataProjects = Config.DataProjects?.FullName;
            var dataProject = Path.Combine(dataProjects, project);

            if (!Directory.Exists(dataProject))
            {
                return NotFound("Project not found");
            }

            var projectSettings = ProjectSettingsService.Get(project, Config);

            if (!TokenService.Check(Request, IConfig, projectSettings, token))
            {
                return BadRequest("User or system token invalid");
            }

            Config.Host = HostService.Get(Request, IConfig);

            var projects = new List<string>() {project};
            var projectsConcatenate = string.Join(',', projects.Select(x => $"'{x}'"));

            var content = @"
                <!DOCTYPE html>
                <html lang=""en"">
                <head>
	                <meta charset=""UTF-8"">
	                <title>Map Openlayers</title>
	                <link rel=""stylesheet"" href=""#url#/css/ol.css"">
	                <link rel=""stylesheet"" href=""#url#/css/app.css"">
                    <link rel=""shortcut icon"" type=""image/x-icon"" href=""../../../swagger-ui/favicon-16x16.ico"" />
	                <style>
		                body { margin: 0px;}
		                .map {
			                width: 100%;
			                height: 100vh;
		                }
	                </style>
                </head>
                <body>
                <div class=""container"">
                  <header></header>
                  <nav><div id=""app-navigation""></div></nav>
                  <main><div id=""map"" class=""map""></div><div id=""crs-display"" class=""crs-display""></div></main>
                  <aside></aside>
                  <footer></footer>
                </div>                
                <script type=""text/javascript"">
                    window.dataMsApp = {}
                    window.dataMsApp.projects = [#projects#]
                    window.dataMsApp.host = '#url#';
                    window.dataMsApp.token = '#token#'
                </script>
                <script type=""module"" src=""#url#/js/app-dist.js""></script>
                </body>
                </html>
            ".Replace("#url#",Config.Host).Replace("#projects#",projectsConcatenate).Replace("#token#", token);

            return new ContentResult()
            {
                Content = content,
                ContentType = "text/html",
                StatusCode = 200
            };
        }

        /// <summary>
        /// Check whether the map file is valid
        /// </summary>
        /// <param name="project">Guid from project</param>
        /// <param name="token">Access token</param>
        /// <returns>result of the check</returns>
        [HttpGet("Check/{project}")]
        public ActionResult CheckMapFile(string project, [FromQuery(Name = "token")] string? token)
        {
            var projectSettings = ProjectSettingsService.Get(project, Config);

            if (!TokenService.Check(Request, IConfig, projectSettings, token))
            {
                return BadRequest("User or system token invalid");
            }

            Config.Host = HostService.Get(Request, IConfig);

            var mapMetadata = MapFileService.GetMapFromProject(project, Config);

            if (!mapMetadata.Succesfully)
            {
                BadRequest("Map not found");
            }

            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };
            string content;

            try
            {
                var mapPath = MapFileService.GetMapPathFromCache(mapMetadata.Value.Key, mapMetadata.Value.FilePath, Config, project) ?? string.Empty;
                var mapServer = new Mapserver(new FileInfo(mapPath));
                content = JsonConvert.SerializeObject(
                    new {status = "map file is ok"},
                    Formatting.None,
                    new JsonSerializerSettings()
                    {
                        ContractResolver = contractResolver,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    }
                );
            }
            catch (Exception ex)
            {
                content = JsonConvert.SerializeObject(
                    ex,
                    Formatting.None,
                    new JsonSerializerSettings()
                    {
                        ContractResolver = contractResolver,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    }
                );
            }

            return new ContentResult()
            {
                Content = content,
                ContentType = "application/json",
                StatusCode = 200
            };
        }

        /// <summary>
        /// Clean cache and temporary folders
        /// </summary>
        /// <param name="project">Guid from project</param>
        /// <param name="token">Access token</param>
        /// <returns>result action was successfully</returns>
        [HttpGet("Cleanup/{project}")]
        public ActionResult CleanupProject(string project, [FromQuery(Name = "token")] string? token)
        {
            var projectSettings = ProjectSettingsService.Get(project, Config);

            if (!TokenService.Check(Request, IConfig, projectSettings, token))
            {
                return BadRequest("User or system token invalid");
            }

            var projectFolder = Config.DataProject(project);

            foreach (var folder in projectFolder.GetDirectories())
            {
                if (!(folder.Name == "_cache" || folder.Name == "_debug" || folder.Name == "_resolvedMapFile"))
                {
                    continue;
                }

                Directory.Delete(folder.FullName, true);
            }

            return Ok();
        }

        [Obsolete]
        [HttpGet("Info/{project}")]
        public ActionResult Info(string project, [FromQuery(Name = "token")] string? token)
        {
            var dataProjects = Config.DataProjects?.FullName;
            var dataProject = Path.Combine(dataProjects, project);

            if (!Directory.Exists(dataProject))
            {
                return NotFound("Project not found");
            }

            var projectSettings = ProjectSettingsService.Get(project, Config);

            if (!TokenService.Check(Request, IConfig, projectSettings, token))
            {
                return BadRequest("User or system token invalid");
            }

            var files = new DirectoryInfo(dataProject).GetFiles("*.*", SearchOption.AllDirectories);
            var fileItems = new List<string>();
            var proxyItems = new Dictionary<string, object>();

            bool mapServerEnabled = false;

            foreach (var file in files)
            {
                if (file.Name.ToLower() == "_projectsettings.json")
                {
                    continue;
                }

                if (file.Extension.ToLower() != ".map" && file.Extension.ToLower() != ".log" && !FileService.IsGisFile(file.FullName))
                {
                    continue; //ignored files
                }

                var proxyFile = $"{file.FullName}.url";

                if (FileService.IsGisFile(file.FullName) && System.IO.File.Exists(proxyFile))
                {
                    var value = JsonConvert.DeserializeObject<WebRequestParameter>(System.IO.File.ReadAllText(proxyFile));
                    proxyItems.Add(proxyFile.Replace($"{dataProject}\\", ""), value);
                }

                if (file.Extension.ToLower() == ".map")
                {
                    mapServerEnabled = true;
                }

                fileItems.Add(file.FullName.Replace($"{dataProject}\\", ""));
            }

            return new ContentResult()
            {
                Content = JsonConvert.SerializeObject(new
                {
                    mapServerEnabled,
                    fileItems,
                    proxyItems,
                }),
                ContentType = "application/json",
                StatusCode = 200
            };
        }
    }
}
