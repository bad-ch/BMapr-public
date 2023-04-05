using BMapr.GDAL.WebApi.Models;

namespace BMapr.GDAL.WebApi.Services
{
    public class ConfigService
    {
        public static string GetUrl(HttpRequest request)
        {
            return string.Concat(
                request.Scheme,
                "://",
                request.Host.ToUriComponent(),
                request.PathBase.ToUriComponent()
                //request.Path.ToUriComponent(),
                //request.QueryString.ToUriComponent()
            );
        }

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
    }
}
