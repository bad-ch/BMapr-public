using BMapr.GDAL.WebApi.Models;
using Microsoft.Extensions.Caching.Memory;
using OSGeo.MapServer;

namespace BMapr.GDAL.WebApi.Services
{
    public class MapFileService
    {
        public static Result<MapConfig> GetServerConfig(string mapContent)
        {
            GdalConfiguration.ConfigureGdal();

            mapObj map;
            var mapConfig = new MapConfig();
            var result = new Result<MapConfig>();

            try
            {
                map = new mapObj(mapContent, 1);

                mapConfig.WFST110enabled = Convert.ToBoolean(map.web.metadata.get("msh_WFST110enabled", "false"));
                mapConfig.WFST110debug = Convert.ToBoolean(map.web.metadata.get("msh_WFST110debug", "false"));
                mapConfig.CacheWmsGetCapabilities = Convert.ToBoolean(map.web.metadata.get("msh_CacheWmsGetCapabilities", "false"));
                mapConfig.CacheWmsGetMap = Convert.ToBoolean(map.web.metadata.get("msh_CacheWmsGetMap", "false"));
                mapConfig.CacheWfsMetadata = Convert.ToBoolean(map.web.metadata.get("msh_CacheWfsMetadata", "false"));
                mapConfig.CacheWfsGetFeature = Convert.ToBoolean(map.web.metadata.get("msh_CacheWfsGetFeature", "false"));

                for (int i = 0; i < map.numlayers; i++)
                {
                    var layer = map.getLayer(i);

                    if (!mapConfig.MapLayers.ContainsKey(layer.name))
                    {
                        mapConfig.MapLayers.Add(layer.name,new MapLayerConfig());
                    }

                    mapConfig.MapLayers[layer.name].WFSTuseMsSqlServerFeatureService = Convert.ToBoolean(layer.metadata.get("msh_WFSTuseMsSqlServerFeatureService", "false"));
                    mapConfig.MapLayers[layer.name].WFSTMsSqlServerGeography = Convert.ToBoolean(layer.metadata.get("msh_WFSTMsSqlServerGeography", "false"));
                    mapConfig.MapLayers[layer.name].IdType = layer.metadata.get("msh_IdType", "");
                    mapConfig.MapLayers[layer.name].IdSquenceManual = Convert.ToBoolean(layer.metadata.get("msh_IdSquenceManual", "false"));
                    mapConfig.MapLayers[layer.name].IdSequenceStartValue = Convert.ToInt32(layer.metadata.get("msh_IdSequenceStartValue", "0"));
                    mapConfig.MapLayers[layer.name].Id = layer.metadata.get("msh_Id", "id");
                    mapConfig.MapLayers[layer.name].Connection = layer.metadata.get("msh_Connection", "");
                    mapConfig.MapLayers[layer.name].EPSG = string.IsNullOrEmpty(layer.getProjection().ToLower()) ? 0 : Convert.ToInt32(layer.getProjection().ToLower().Replace("init=epsg:",""));
                }

                result.Value = mapConfig;
                result.Succesfully = true;
            }
            catch (Exception ex)
            {
                result.Succesfully = false;
                result.AddMessage("Get config from ma file FAIL", ex);
            }

            return result;
        }

        public static Result<MapConfig> GetMapConfigFromCache(IMemoryCache cache, string mapKey, string mapFile, Config config, string project)
        {
            var cacheMapConfig = cache.Get("mapConfig") as Dictionary<string, MapConfig>;

            if (cacheMapConfig == null)
            {
                return new Result<MapConfig>(){Messages = new List<string>(){ "cacheMapConfig == null" } };
            }

            string? mapContent = GetMapContentFromCache(cache, mapKey, mapFile, config, project);

            if (string.IsNullOrEmpty(mapContent))
            {
                return new Result<MapConfig>() { Messages = new List<string>() { "string.IsNullOrEmpty(mapContent)" } };
            }

            var result = new Result<MapConfig>();

            if (cacheMapConfig.ContainsKey(mapKey))
            {
                result.Value = cacheMapConfig[mapKey];
                result.Succesfully = true;
                result.Messages.Add("Get server config from cache");
            }
            else
            {
                var resultServerConfig = GetServerConfig(mapContent);

                result.Value = resultServerConfig.Value;
                result.Succesfully = resultServerConfig.Succesfully;
                result.TakeoverEvents(resultServerConfig);

                if (resultServerConfig.Succesfully)
                {
                    cacheMapConfig.Add(mapKey, result.Value);
                }
            }

            return result;
        }

        public static string? GetMapContentFromCache(IMemoryCache cache, string mapKey, string mapFile, Config config, string project)
        {
            var cacheMapContent = cache.Get("mapContent") as Dictionary<string, string>;

            if (cacheMapContent == null)
            {
                return null;
            }

            string mapContent;
            var resolvedMapFilePath = Path.Combine(config.DataProject(project).FullName, "_resolvedMapFile");

            if (!Directory.Exists(resolvedMapFilePath))
            {
                Directory.CreateDirectory(resolvedMapFilePath);
            }

            if (cacheMapContent.ContainsKey(mapKey))
            {
                mapContent = cacheMapContent[mapKey];
            }
            else
            {
                mapContent = File.ReadAllText(mapFile);

                mapContent = mapContent.Replace("#host#", config.Host);
                mapContent = mapContent.Replace("#project#", project);
                mapContent = mapContent.Replace("#dataPath#", config.Data?.FullName.Replace(@"\","/"));
                mapContent = mapContent.Replace("#projectPath#", config.DataProject(project).FullName.Replace(@"\", "/"));

                File.WriteAllText(Path.Combine(resolvedMapFilePath, $"mapfile_{project}_{Guid.NewGuid()}_resolved.map"), mapContent);

                cacheMapContent.Add(mapKey, mapContent);
            }

            return mapContent;
        }

        public static Result<MapMetadata> GetMapFromProject(string projectMap, Config config)
        {
            var result = new Result<MapMetadata>() {Succesfully = false};
            var project = projectMap;
            var mapName = "";

            if (projectMap.Contains("_mshMap_"))
            {
                project = projectMap.Split("_mshMap_")[0];
                mapName = $"{projectMap.Split("_mshMap_")[1]}.map";
            }

            if (string.IsNullOrEmpty(project))
            {
                result.AddMessage("No valid map file name");
                return result;
            }

            var projectsPath = config.DataProjects;

            if (projectsPath == null || !Directory.Exists(projectsPath.FullName))
            {
                result.AddMessage("No valid root path");
                return result;
            }

            var projectPath = Path.Combine(projectsPath.FullName, $"{project}");

            if (!Directory.Exists(projectPath))
            {
                result.AddMessage("No valid data path");
                return result;
            }

            var mapFileInfo = new DirectoryInfo(projectPath).GetFiles("*.map").FirstOrDefault();

            if (mapFileInfo == null)
            {
                throw new Exception("No map file found in the project folder");
            }

            if (!string.IsNullOrEmpty(mapName))
            {
                var mapFileInfoOtherMap = new DirectoryInfo(projectPath).GetFiles(mapName).FirstOrDefault();

                if (mapFileInfoOtherMap != null)
                {
                    result.AddMessage($"Take map file <<{mapName}>>");
                    mapFileInfo = mapFileInfoOtherMap;
                }
                else
                {
                    result.AddMessage($"Map file <<{mapName}>> not found");
                }
            }
            else
            {   
                result.AddMessage($"Take first map file <<{mapFileInfo?.Name}>>");
            }

            var mapFile = mapFileInfo?.FullName;
            var mapFileName = mapFileInfo?.Name;

            if (mapFile == null || !System.IO.File.Exists(mapFile))
            {
                result.AddMessage("Map file not found");
                return result;
            }

            result.Value = new MapMetadata();

            result.Value.HostKey = config.Host.Replace(":","_").Replace("/","_").Replace("\\","");
            result.Value.MapKey = mapFileName.Replace(".map", "");
            result.Value.LastWriteTimeKey = (new FileInfo(mapFile)).LastWriteTime.ToString("yyyMMdd_HHmmss");
            result.Value.Key = $"{result.Value.HostKey}_{result.Value.MapKey}_{result.Value.LastWriteTimeKey}";
            result.Value.FilePath = mapFile;
            result.Succesfully = true;
            return result;
        }
    }
}
