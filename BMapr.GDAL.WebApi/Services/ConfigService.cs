using BMapr.GDAL.WebApi.Models;
using OSGeo.MapServer;

namespace BMapr.GDAL.WebApi.Services
{
    public static class ConfigService
    {
        public static Dictionary<string, mapObj> MapObjCache { get; set; } = new Dictionary<string, mapObj>();

        public static Config Get(IConfiguration iConfig, IWebHostEnvironment environment)
        {
            var config = new Config();

            string data = iConfig.GetSection("Settings").GetSection("Data").Value;
            string dataProjects = iConfig.GetSection("Settings").GetSection("DataProjects").Value;
            string dataShare = iConfig.GetSection("Settings").GetSection("DataShare").Value;
            
            config.AssemblyPath = PathService.AssemblyDirectory();
            config.ApplicationRoot = new DirectoryInfo(environment.ContentRootPath);

            config.Data = data == "#data#" ? new DirectoryInfo(Path.Combine(config.ApplicationRoot.FullName,"data")) : new DirectoryInfo(data);//set absolute path

            if (config.Data == null || !Directory.Exists(config.Data.FullName))
            {
                return new Config();
            }

            var dataProjectsString = Path.Combine(config.Data.FullName, dataProjects);

            if (!Directory.Exists(dataProjectsString))
            {
                return new Config();
            }

            config.DataProjects = new DirectoryInfo(dataProjectsString);

            var dataShareString = Path.Combine(config.Data.FullName, dataShare);

            if (!Directory.Exists(dataShareString))
            {
                return new Config();
            }

            config.DataShare = new DirectoryInfo(dataShareString);

            config.Temp = new DirectoryInfo(Path.GetTempPath());

            config.Cache = iConfig.GetSection("Settings").GetSection("Cache").Value != null && iConfig.GetSection("Settings").GetSection("Cache").Value.ToLower() == "true";

            return config;
        }

        public static mapObj GetMapObject(string mapContent)
        {
            var hashRequest = HashService.CreateFromStringMd5(mapContent);

            if (MapObjCache.ContainsKey(hashRequest))
            {
                return MapObjCache[hashRequest];
            }

            var map = new mapObj(mapContent, 1);

            try
            {
                MapObjCache.Add(hashRequest, map);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dictionary not thread safe {ex.Message}");
            }

            return map;
        }
    }
}
