using BMapr.GDAL.WebApi.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Services
{
    public class ProjectSettingsService
    {
        public static string FileName()
        {
            return "_projectSettings.json";
        }

        public static ProjectSettings? Get(string guid, Config config)
        {
            var projectFolder = Path.Combine(config.DataProjects.FullName, guid);

            if (!Directory.Exists(config.DataProjects.FullName) || !Directory.Exists(projectFolder))
            {
                return new ProjectSettings()
                {
                    Token = Guid.Empty,
                    Message = $"DataProjects {config.DataProjects.FullName} or project folder {projectFolder} don't exist"
                };
            }

            var projectSettingsPath = Path.Combine(projectFolder, FileName());

            if (!File.Exists(projectSettingsPath))
            {
                return new ProjectSettings()
                {
                    Token = Guid.Empty,
                    Message = $"Project settings {projectSettingsPath} file don't exists"
                };
            }

            return JsonConvert.DeserializeObject<ProjectSettings>(File.ReadAllText(projectSettingsPath));
        }

        public static ProjectSettings CreateNew(Guid guid, Config config)
        {
            var projectFolder = Path.Combine(config.DataProjects.FullName, guid.ToString());

            if (!Directory.Exists(config.DataProjects.FullName) || !Directory.Exists(projectFolder))
            {
                return new ProjectSettings()
                {
                    Token = Guid.Empty,
                    Message = $"DataProjects {config.DataProjects.FullName} or project folder {projectFolder} don't exist"
                };
            }

            var projectSettingsPath = Path.Combine(projectFolder, FileName());

            if (File.Exists(projectSettingsPath))
            {
                return new ProjectSettings()
                {
                    Token = Guid.Empty,
                    Message = $"Project settings {projectSettingsPath} file already exists"
                };
            }

            var projectsSettings = new ProjectSettings();

            File.WriteAllText(projectSettingsPath, JsonConvert.SerializeObject(projectsSettings));

            return projectsSettings;
        }
    }
}
