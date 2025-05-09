using BMapr.GDAL.WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BMapr.GDAL.WebApi.Services
{
    public class CacheService
    {
        public const string CACHEPATH = "_cache";
        public const string CONTENTTYPEPATH = "_contentType";

        public bool CacheEnabled { get; set; } = true;

        public string ContentPath => _ContentPath;
        public string ContentTypePath => _ContentTypePath;

        private string _ContentPath { get; set; }
        private string _ContentTypePath { get; set; }
        private string _Host { get; set; }

        public CacheService(string basePath, string host, string service, string collectionId)
        {
            _Host = host.Replace(":","_").Replace("//",string.Empty).Replace("/","_").Replace("\\","");
            _ContentPath = Path.Combine(basePath, CACHEPATH, _Host, service, collectionId);
            _ContentTypePath = Path.Combine(_ContentPath, CONTENTTYPEPATH);

            if (!Directory.Exists(_ContentPath))
            {
                Directory.CreateDirectory(_ContentPath);
            }

            if (!Directory.Exists(_ContentTypePath))
            {
                Directory.CreateDirectory(_ContentTypePath);
            }
        }

        public void WriteContent(string url, byte[] content, string contentType)
        {
            if (!CacheEnabled)
            {
                return;
            }

            var hashRequest = HashService.CreateFromStringMd5(url);

            var filePath = Path.Combine(_ContentPath, hashRequest);
            var fileContentTypePath = Path.Combine(_ContentTypePath, hashRequest);

            File.WriteAllBytes(filePath, content);
            File.WriteAllText(fileContentTypePath, contentType);
        }

        public FileContentResult? GetContent(string url)
        {
            if (!CacheEnabled)
            {
                return null;
            }

            var hashRequest = HashService.CreateFromStringMd5(url);

            var filePath = Path.Combine(_ContentPath, hashRequest);
            var fileContentTypePath = Path.Combine(_ContentTypePath, hashRequest);

            if (!File.Exists(filePath) || !File.Exists(fileContentTypePath))
            {
                return null;
            }

            var fileContent = File.ReadAllBytes(filePath);
            var fileContentType = File.ReadAllText(fileContentTypePath);
            return new FileContentResult(fileContent, fileContentType);
        }

        // old stuff
        public static bool RequestCached(Config config, MapConfig mapConfig, string serviceParameter, string requestParameter)
        {
            if (config.Cache != null && !(bool)config.Cache)
            {
                return false;
            }

            if (serviceParameter == "wfs")
            {
                switch (requestParameter)
                {
                    case "getcapabilities":
                    case "describefeaturetype":
                        return mapConfig.CacheWfsMetadata;
                    case "getfeature":
                        return mapConfig.CacheWfsGetFeature;
                    default:
                        return false;
                }
            }

            if (serviceParameter == "wms")
            {
                switch (requestParameter)
                {
                    case "getcapabilities":
                        return mapConfig.CacheWmsGetCapabilities;
                    case "getmap":
                        return mapConfig.CacheWmsGetMap;
                    default:
                        return false;
                }
            }

            return false;
        }
    }
}
