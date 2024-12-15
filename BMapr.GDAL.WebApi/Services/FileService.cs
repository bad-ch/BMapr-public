using System.Net.Http.Headers;
using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.Net;

namespace BMapr.GDAL.WebApi.Services
{
    public class FileService
    {
        //private static readonly ILogger<FileService> _logger;

        public static bool AllowedFiles(string filename)
        {
            var extension = Path.GetExtension(filename).ToLower();

            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            var allowedFiles = new List<string>() {".shp",".map",".prj",".shx",".dbf",".txt",".json",".geojson",".dxf",".csv",".gpkg",".xml",".ili",".itf",".imd",".kml",".gml",".zip","kmz"};
            return allowedFiles.Contains(extension);
        }

        public static string GetExtension(string filename)
        {
            return Path.GetExtension(filename).ToLower();
        }

        public static bool IsGisFile(string filename)
        {
            var extension = Path.GetExtension(filename).ToLower();

            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            var allowedFiles = new List<string>() { ".shp", ".json", ".geojson", ".dxf", ".csv", ".gpkg", ".xml", ".kml", ".gml", "kmz" };
            return allowedFiles.Contains(extension);
        }

        public static string GetMimeType(string filename)
        {
            var extension = Path.GetExtension(filename).ToLower();

            if (string.IsNullOrEmpty(extension))
            {
                return null;
            }

            switch (extension)
            {
                case ".json":
                case ".geojson":
                    return "application/json";
                case ".gml":
                case ".xml":
                    return "text/xml";
                case ".csv":
                    return "application/csv";
                case ".kml":
                    return "application/vnd.google-earth.kml+xml";
                case ".kmz":
                    return "application/vnd.google-earth.kmz";
                case ".txt":
                    return "text/plain";
                case ".gpkg":
                    return "application/geopackage+sqlite3";
                case ".fgb":
                    return "application/octet-stream";
                default:
                    return null;
            }
        }

        public static bool ForbiddenFiles(string filename)
        {
            return !AllowedFiles(filename);
        }

        public static Result<TaskCopyProject> CopyDataToProjectFolder(HttpRequest request, string dataProject)
        {
            var result = new Result<TaskCopyProject>() {Succesfully = false, Value = new TaskCopyProject()};

            try
            {
                foreach (var file in request.Form.Files)
                {
                    if (file.Length > 0)
                    {
                        var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');

                        if (string.IsNullOrEmpty(fileName))
                        {
                            result.AddMessage("file name invalid");
                            //_logger.LogError("file name invalid");
                            continue;
                        }

                        if (FileService.AllowedFiles(fileName))
                        {
                            result.Value.AllowedFiles.Add($"{fileName}");
                        }
                        else
                        {
                            result.Value.ForbiddenFiles.Add($"{fileName}");
                            continue;
                        }

                        var extension = Path.GetExtension(fileName).ToLower();
                        var filePath = Path.Combine(extension == ".zip" ? Path.GetTempPath() : dataProject, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }

                        if (extension == ".zip")
                        {
                            var resultZip = ZipService.Extract(Path.Combine(Path.GetTempPath(), fileName), dataProject);

                            if (!resultZip.Succesfully)
                            {
                                result.AddMessage("unzip archive fail");
                                //_logger.LogError("unzip archive fail");
                            }

                            result.Value.AllowedFiles.AddRange(resultZip.Messages.Where(x => x.Contains("allowed")).Select(x => $"zip:{x.Split(':')[1]}"));
                            result.Value.ForbiddenFiles.AddRange(resultZip.Messages.Where(x => x.Contains("forbidden")).Select(x => $"zip:{x.Split(':')[1]}"));
                        }
                    }
                }

                if (result.Value.AllowedFiles.Count == 0)
                {
                    result.Messages.Add("remove created directory");
                    Directory.Delete(dataProject);
                }

                result.Succesfully = true;
            }
            catch (Exception ex)
            {
                //_logger.LogError($"Internal server error: {ex}");

                result.Exceptions.Add(ex);
                result.Messages.Add($"Internal server error, {ex.Message}");
            }

            return result;
        }

        public static Result<bool> CleanupProjectFolder(string dataProject)
        {
            var result = new Result<bool>() { Succesfully = false };
            var projectFolder = new DirectoryInfo(dataProject);
            var trashName = $"_trash_{Guid.NewGuid()}";
            var trash = Path.Combine(projectFolder.FullName, trashName);

            Directory.CreateDirectory(trash);

            var statusMoveData = MoveData(projectFolder.FullName, trash, trashName);

            if (!statusMoveData.Succesfully)
            {
                // restore already moved data
                MoveData(trash, projectFolder.FullName, "");
                Directory.Delete(trash, true);
                result.Exceptions.AddRange(statusMoveData.Exceptions);
                return result;
            }

            // delete fully moved data
            Directory.Delete(trash, true);
            result.Succesfully = true;
            return result;
        }

        private static Result<bool> MoveData(string source, string destination, string name)
        {
            var result = new Result<bool>() {Succesfully = false};
            var sourceFolder = new DirectoryInfo(source);

            try
            {
                foreach (var file in sourceFolder.GetFiles())
                {
                    if (file.Name.ToLower() == "_projectsettings.json")
                    {
                        continue;
                    }

                    File.Move(file.FullName, Path.Combine(destination, file.Name));
                }

                foreach (var directory in sourceFolder.GetDirectories())
                {
                    if (directory.Name == name)
                    {
                        continue;
                    }

                    Directory.Move(directory.FullName, Path.Combine(destination, directory.Name));
                }
            }
            catch (Exception ex)
            {
                //_logger.LogError($"Error move project data: {source}, {destination}, {ex}");
                result.Exceptions.Add(ex);
                return result;
            }

            result.Succesfully = true;
            return result;
        }

        public static void UpdateMapFile(string dataProject, string project, Config config, string url)
        {
            var mapFile = new DirectoryInfo(dataProject).GetFiles("*.map").FirstOrDefault();

            if (mapFile != null)
            {
                var mapContent = File.ReadAllText(mapFile.FullName);

                mapContent = mapContent.Replace("#urlProject#", $"{url}/api/ogc/interface/{project}");

                File.WriteAllText(mapFile.FullName, mapContent);
            }
        }

        public static Result<RequestContent> GetContent(HttpRequest request)
        {
            var result = new Result<RequestContent>()
            {
                Value = new RequestContent(),
            };

            foreach (var file in request.Form.Files)
            {
                if (file.Length > 0)
                {
                    var fileContent = new FileContent();

                    fileContent.Name = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');

                    if (string.IsNullOrEmpty(fileContent.Name))
                    {
                        result.AddMessage("file name invalid");
                        continue;
                    }

                    using (var ms = new MemoryStream())
                    {
                        file.CopyTo(ms);
                        fileContent.Data = ms.ToArray();
                        fileContent.Size = ms.Length;
                    }

                    result.Value.Files.Add(fileContent);
                }
            }
            foreach (var item in request.Form)
            {
                if (result.Value.Values.ContainsKey(item.Key))
                {
                    continue;
                }

                result.Value.Values.Add(item.Key, string.Join(",", item.Value.ToArray()));
            }

            return result;
        }
    }
}
