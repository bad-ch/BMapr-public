using BMapr.GDAL.WebApi.Models.Spatial;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Services
{
    public static class CrsService
    {
        public static List<CrsDefinition> GetData(ILogger logger, IConfiguration iConfig, IWebHostEnvironment environment)
        {
            var config = ConfigService.Get(iConfig, environment);
            var dataSharePath = config.DataShare;
            var pathCrsFile = Path.Combine(dataSharePath.FullName, $"CrsDefinitionList.json");
            List<CrsDefinition> data = new();

            if (!File.Exists(pathCrsFile))
            {
                logger.LogError($"crs file not found: {pathCrsFile}");
                return data;
            }

            var crsDefinitionsString = System.IO.File.ReadAllText(pathCrsFile);

            try
            {
                var crsDefinitions = JsonConvert.DeserializeObject<List<CrsDefinition>>(crsDefinitionsString);

                if (crsDefinitions != null)
                {
                    return crsDefinitions;
                }

            }
            catch (Exception ex)
            {
                logger.LogError($"DES error: {pathCrsFile}, {ex.Message}");
            }

            return data;
        }
    }
}
