using BMapr.GDAL.WebApi.Models;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Services
{
    public class ProjectSettingsService
    {
        private static ILogger<ProjectSettingsService> _logger;

        public static string FileName()
        {
            return "_projectSettings.json";
        }

        public static ProjectSettings? Get(string guid, Config config)
        {
            var projectFolder = Path.Combine(config.DataProjects.FullName, guid);

            if (!Directory.Exists(config.DataProjects.FullName) || !Directory.Exists(projectFolder))
            {
                _logger.LogError($"DataProjects {config.DataProjects.FullName} or project folder {projectFolder} don't exist");
                return null;
            }

            var projectSettingsPath = Path.Combine(projectFolder, FileName());

            if (!File.Exists(projectSettingsPath))
            {
                _logger.LogError($"Project settings {projectSettingsPath} file don't exists");
                throw new Exception("Project settings are not available");
            }

            return JsonConvert.DeserializeObject<ProjectSettings>(File.ReadAllText(projectSettingsPath));
        }

        public static ProjectSettings CreateNew(Guid guid, Config config)
        {
            var projectFolder = Path.Combine(config.DataProjects.FullName, guid.ToString());

            if (!Directory.Exists(config.DataProjects.FullName) || !Directory.Exists(projectFolder))
            {
                _logger.LogError($"DataProjects {config.DataProjects.FullName} or project folder {projectFolder} don't exist");
                return null;
            }

            var projectSettingsPath = Path.Combine(projectFolder, FileName());

            if (File.Exists(projectSettingsPath))
            {
                _logger.LogError($"Project settings {projectSettingsPath} file already exists");
                return null;
            }

            var projectsSettings = new ProjectSettings();

            File.WriteAllText(projectSettingsPath, JsonConvert.SerializeObject(projectsSettings));

            return projectsSettings;
        }
    }
}
