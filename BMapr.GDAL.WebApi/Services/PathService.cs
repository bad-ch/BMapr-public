using System.Reflection;

namespace BMapr.GDAL.WebApi.Services
{
    public class PathService
    {
        public static string AssemblyDirectoryString()
        {
            var location = Assembly.GetExecutingAssembly().Location;
            var locationFileInfo = new FileInfo(location);
            return locationFileInfo?.Directory?.FullName;
        }

        public static DirectoryInfo AssemblyDirectory()
        {
            return new DirectoryInfo(AssemblyDirectoryString());
        }

    }
}
