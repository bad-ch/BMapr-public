using BMapr.GDAL.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using OSGeo.MapServer;
using System.Web;

namespace BMapr.GDAL.WebApi.Services
{
    public class OgcService
    {
        //private static ILogger<OgcService> _logger;

        public const string CACHEPATH = "_cache";

        public enum RequestType
        {
            Get,
            Post
        }

        public static ActionResult Process(RequestType requestType, string queryString, string body, Config config, string project, string bodyContentType = "")
        {
            var mapMetadata = MapFileService.GetMapFromProject(project, config);

            if (!mapMetadata.Succesfully || string.IsNullOrEmpty(mapMetadata.Value.Key) || string.IsNullOrEmpty(mapMetadata.Value.FilePath))
            {
                return new BadRequestResult();
            }

            var resultMapConfig = MapFileService.GetMapConfigFromCache(mapMetadata.Value.Key, mapMetadata.Value.FilePath, config, project);

            if (!resultMapConfig.Succesfully)
            {
                return new BadRequestResult();
            }

            var mapConfig = resultMapConfig.Value;

            var projectsPath = config.DataProjects;
            var projectPath = Path.Combine(projectsPath.FullName, $"{project}");
            var filePath = string.Empty;
            var filePathContentType = string.Empty;

            var queryStringLowerCase = queryString.ToLower();
            var queryStringParameters = HttpUtility.ParseQueryString(queryStringLowerCase);
            var serviceParameter = queryStringParameters["service"] ?? "service";
            var requestParameter = queryStringParameters["request"] ?? "request";
            var version = queryStringParameters["version"] ?? "version";

            var cacheActive = CacheService.RequestCached(config, mapConfig, serviceParameter, requestParameter);

            if (cacheActive && requestType == RequestType.Get)
            {
                var cacheFolder = string.IsNullOrEmpty(mapConfig.CacheWmsGetMapPath) ? Path.Combine(projectPath, $"{CACHEPATH}/{mapMetadata.Value.Key}/{serviceParameter}/{requestParameter}") : Path.Combine(mapConfig.CacheWmsGetMapPath, $"{mapMetadata.Value.Key}/{serviceParameter}/{requestParameter}");
                var cacheFolderContentType = Path.Combine(cacheFolder, "_contentType");

                if (!Directory.Exists(cacheFolder))
                {
                    Directory.CreateDirectory(cacheFolder);
                    Directory.CreateDirectory(cacheFolderContentType);
                }

                var hashRequest = HashService.CreateFromStringMd5(queryString);

                filePath = Path.Combine(cacheFolder, hashRequest);
                filePathContentType = Path.Combine(cacheFolderContentType, hashRequest);

                if (File.Exists(filePath))
                {
                    var fileContent = File.ReadAllBytes(filePath);
                    var fileContentType = File.ReadAllText(filePathContentType);
                    return new FileContentResult(fileContent, fileContentType);
                }
            }

            var path = GdalConfiguration.IniPath();
            mapscript.SetEnvironmentVariable(path);

            string? contentType = "";
            byte[] contentBytes = new byte[]{};

            OWSRequest req = new OWSRequest();
            mapObj map = null;

            try
            {
                map = new mapObj(mapConfig.MapPath);

                if (requestType == RequestType.Get)
                {
                    req.type = MS_REQUEST_TYPE.MS_GET_REQUEST;
                    req.loadParamsFromURL(queryString);
                }
                else
                {
                    req.type = MS_REQUEST_TYPE.MS_POST_REQUEST;
                    req.contenttype = bodyContentType;
                    req.postrequest = body;
                }

                mapscript.msIO_installStdoutToBuffer();

                int result = map.OWSDispatch(req);

                if (result != 0)
                {
                    map.Dispose();
                    req.Dispose();
                    mapscript.msIO_resetHandlers();
                    return new BadRequestObjectResult("Wrong result from map");
                }

                contentType = mapscript.msIO_stripStdoutBufferContentType().Split(';')[0];
                contentBytes = mapscript.msIO_getStdoutBufferBytes();
                map.Dispose();
                req.Dispose();
                mapscript.msIO_resetHandlers();
            }
            catch (Exception ex)
            {
                if (map != null)
                {
                    map.Dispose();
                }
                req.Dispose();
                mapscript.msIO_resetHandlers();
                return new BadRequestObjectResult($"error: {ex.Message}");
            }

            if (serviceParameter == "wfs" && requestParameter == "getcapabilities" && version == "1.1.0" && mapConfig != null && mapConfig.WFST110enabled)
            {
                // DEBUG only: File.WriteAllText(Path.Combine(Path.GetTempPath(),$"{guid2}.xml"), Encoding.UTF8.GetString(contentBytes));
                contentBytes = WfsService.AddTransactionToGetCapabilities(contentBytes);
                // DEBUG only: File.WriteAllText(Path.Combine(Path.GetTempPath(), $"{guid2}_mod.xml"), Encoding.UTF8.GetString(contentBytes));
            }

            if (cacheActive && requestType == RequestType.Get)
            {
                File.WriteAllBytes(filePath, contentBytes);
                File.WriteAllText(filePathContentType, contentType);
            }

            return new FileContentResult(contentBytes, contentType);
        }
    }
}
