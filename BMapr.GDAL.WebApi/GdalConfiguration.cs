using OSGeo.MapServer;
using OSGeo.OSR;
using System.Reflection;
using Gdal = OSGeo.GDAL.Gdal;
using Ogr = OSGeo.OGR.Ogr;

namespace BMapr.GDAL.WebApi
{
    public static partial class GdalConfiguration
    {
        private static bool _configuredOgr;
        private static bool _configuredGdal;
        private static bool _iniPath;

        private static string _usedPath { get; set; }
        public static string UsedPath => _usedPath;

        /// <summary>
        /// set path, par example testing
        /// </summary>
        public static string ExternalPath { get; set; } = "";

        /// <summary>
        /// Function to determine which platform we're on
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">thrown when target architecture is wrong</exception>
        private static string GetPlatform()
        {
            var plattform = IntPtr.Size == 4 ? "x86" : "x64";
            if ("x86".Equals(plattform))
            {
                const string msg = "BMapr.GDAL is only supported on x64 change your plattform target from 'Any CPU' or 'x86' to 'x64'. You may alternative disable the prefere 32-bit option!";
                throw new PlatformNotSupportedException(msg);
            }
            return plattform;
        }

        /// <summary>
        /// Construction of Gdal/Ogr
        /// </summary>
        /// <exception cref="TypeInitializationException">thrown when target architecture is wrong or gdal libs cannot be found.</exception>
        public static string IniPath()
        {
            string executingDirectory;

            if (string.IsNullOrEmpty(ExternalPath))
            {
                var executingAssemblyFile = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath;
                executingDirectory = Path.GetDirectoryName(executingAssemblyFile);
                _usedPath = executingDirectory;
            }
            else
            {
                executingDirectory = ExternalPath;
                _usedPath = ExternalPath;
            }

            if (string.IsNullOrEmpty(executingDirectory))
                throw new InvalidOperationException("cannot get executing directory");


            var gdalPath = Path.Combine(executingDirectory, "gdal");

            string nativePath;
            var plattform = GetPlatform();
            nativePath = Path.Combine(gdalPath, plattform);

            var path = Environment.GetEnvironmentVariable("PATH");
            path = nativePath + ";" + Path.Combine(nativePath, "plugins") + ";" + path;
            Environment.SetEnvironmentVariable("PATH", path);

            var gdalData = Path.Combine(gdalPath, "data");
            Environment.SetEnvironmentVariable("GDAL_DATA", gdalData);
            Gdal.SetConfigOption("GDAL_DATA", gdalData);


            var driverPath = Path.Combine(nativePath, "plugins");
            Environment.SetEnvironmentVariable("GDAL_DRIVER_PATH", driverPath);
            Gdal.SetConfigOption("GDAL_DRIVER_PATH", driverPath);

            Environment.SetEnvironmentVariable("GEOTIFF_CSV", gdalData);
            Gdal.SetConfigOption("GEOTIFF_CSV", gdalData);

            var projSharePath = Path.Combine(gdalPath, "share");
            Environment.SetEnvironmentVariable("PROJ_LIB", projSharePath);
            Gdal.SetConfigOption("PROJ_LIB", projSharePath);

            Osr.SetPROJSearchPath(projSharePath);

            _iniPath = true;

            return nativePath;
        }

        /// <summary>
        /// Method to ensure the static constructor is being called.
        /// </summary>
        /// <remarks>Be sure to call this function before using Gdal/Ogr/Osr</remarks>
        public static void ConfigureOgr()
        {
            if (!_iniPath)
            {
                IniPath();
            }

            if (_configuredOgr)
            {
                return;
            }

            Ogr.RegisterAll();
            _configuredOgr = true;
        }

        /// <summary>
        /// Method to ensure the static constructor is being called.
        /// </summary>
        /// <remarks>Be sure to call this function before using Gdal/Ogr/Osr</remarks>
        public static void ConfigureGdal()
        {
            if (!_iniPath)
            {
                IniPath();
            }

            if (_configuredGdal)
            {
                return;
            }

            Gdal.AllRegister();
            _configuredGdal = true;
        }

        public static List<string> GetDriversOgr()
        {
            ConfigureOgr();

            var drivers = new List<string>();
            var num = Ogr.GetDriverCount();

            for (var i = 0; i < num; i++)
            {
                var driver = Ogr.GetDriver(i);
                drivers.Add($"{driver.name}");
            }

            return drivers;
        }

        public static List<string> GetDriversGdal()
        {
            ConfigureGdal();

            var drivers = new List<string>();
            var num = Gdal.GetDriverCount();

            for (var i = 0; i < num; i++)
            {
                var driver = Gdal.GetDriver(i);
                drivers.Add($"{driver.ShortName} - {driver.LongName}");
            }

            return drivers;
        }

        public static string GetVersionGdal()
        {
            ConfigureGdal();
            return Gdal.VersionInfo("");
        }

        public static string GetVersionOgr()
        {
            return $"Ogr Geos Version {Ogr.GetGEOSVersionMajor()}.{Ogr.GetGEOSVersionMinor()}.{Ogr.GetGEOSVersionMicro()}";
        }

        public static string GetVersionMapserver()
        {
            ConfigureGdal();
            ConfigureOgr();
            return mapscript.MS_VERSION;
        }

        public static Dictionary<string, List<string>> GetCompiledOptionsMapserver()
        {
            ConfigureGdal();
            ConfigureOgr();

            var options = mapscript.msGetVersion();
            var results = new Dictionary<string, List<string>>();

            foreach (var option in options.Split(' ').ToList())
            {
                if (!option.Contains("="))
                {
                    continue;
                }

                var parts = option.Split('=');

                if (!results.ContainsKey(parts[0]))
                {
                    results.Add(parts[0], new List<string>());
                }

                results[parts[0]].Add($"{parts[1]}");
            }

            return results;
        }
    }
}