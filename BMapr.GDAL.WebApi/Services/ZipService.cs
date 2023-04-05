using System.IO.Compression;
using System.Text;
using BMapr.GDAL.WebApi.Models;

namespace BMapr.GDAL.WebApi.Services
{
    public class ZipService
    {
        public static Result Extract(string pathZipFile, string folderUnzip)
        {
            var result = new Result() {Succesfully = false};

            if (!File.Exists(pathZipFile) || !Directory.Exists(folderUnzip))
            {
                result.AddMessage("zip file or extract path does not exist");
                return result;
            }

            try
            {
                using (Stream stream = File.Open(pathZipFile, FileMode.Open))
                {
                    using (ZipArchive archive = new ZipArchive(stream))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            string fullPath = Path.GetFullPath(Path.Combine(folderUnzip, entry.FullName));

                            if (Path.GetFileName(fullPath).Length != 0)
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                                if (FileService.AllowedFiles(fullPath))
                                {
                                    result.Messages.Add($"allowed unzipped file:{fullPath.Replace(folderUnzip, "")}");
                                }
                                else
                                {
                                    result.Warning = true;
                                    result.Messages.Add($"forbidden file in zip:{fullPath.Replace(folderUnzip, "")}");
                                    continue;
                                }

                                using (Stream fileStream = File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    using (Stream entryStream = entry.Open())
                                    {
                                        entryStream.CopyTo(fileStream);
                                    }
                                }
                            }
                            else
                            {
                                Directory.CreateDirectory(fullPath);
                            }
                        }
                    }
                }

                result.Succesfully = true;
            }
            catch (Exception ex)
            {
                result.Exceptions.Add(ex);
                result.Messages.Add("unzip file fail");
            }

            return result;
        }

        public static byte[] Archive(string directoryPath)
        {
            var zipFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");

            ZipFile.CreateFromDirectory(
                directoryPath,
                zipFilePath,
                compressionLevel: CompressionLevel.Optimal,
                includeBaseDirectory: false,
                entryNameEncoding: Encoding.UTF8);

            var fileStream = new FileStream(zipFilePath, FileMode.Open);

            var memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);
            
            using (memoryStream)
            {
                return memoryStream.ToArray();
            }
        }
    }
}
