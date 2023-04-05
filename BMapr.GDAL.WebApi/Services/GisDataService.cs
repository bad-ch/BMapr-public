using BMapr.GDAL.WebApi.Models.Spatial;
using Newtonsoft.Json;
using OSGeo.OGR;

namespace BMapr.GDAL.WebApi.Services
{
    /// <summary>
    /// Convert OGR GIS data in different formats
    /// </summary>
    public class GisDataService
    {
        public static bool Convert(string filenameSource, string filenameTarget, int crsSource = 0, int crsTarget = 0, string layerName = "", int forceDim = -1, bool skipfailures = false)
        {
            if (!File.Exists(filenameSource))
            {
                //log.Error($"file not found {filenameSource}");
                return false;
            }

            var args = new List<string>();

            var formatTarget = GetFormatFromFile(filenameTarget);

            if (string.IsNullOrEmpty(formatTarget))
            {
                //log.Error($"Does not recognize the format of the target file {filenameTarget}");
                return false;
            }

            if (formatTarget == "FlatGeobuf" || skipfailures)
            {
                args.Add("-skipfailures"); //todo needed for flatgeobuf
            }

            args.AddRange(new List<string>() { "-f", formatTarget });

            if (crsSource > 0)
            {
                args.AddRange(new List<string>() { "-s_srs", $"EPSG:{crsSource}" });
            }

            if (crsTarget > 0)
            {
                args.AddRange(new List<string>() { "-t_srs", $"EPSG:{crsTarget}" });
            }

            if (formatTarget == "CSV")
            {
                args.AddRange(new List<string>() { "-lco", "GEOMETRY=AS_WKT" });
            }

            args.AddRange(new List<string>() { "-fieldTypeToString", "StringList" });

            args.Add(filenameTarget);
            args.Add(filenameSource);

            if (!string.IsNullOrEmpty(layerName))
            {
                args.AddRange(new List<string>() { "-nln", layerName });
            }

            if (formatTarget == "FlatGeobuf")
            {
                args.AddRange(new List<string>() { "-nlt", "GEOMETRY" }); //todo needed for flatgeobuf
            }

            try
            {
                GdalConfiguration.ConfigureOgr();
                Ogr2OgrService.Execute(args.ToArray(), forceDim);
            }
            catch (Exception ex)
            {
                //log.Error($"error ogr2ogr {filenameSource} to {filenameTarget}, message {ex.Message}");
                return false;
            }

            return true;
        }

        public static string GetFormatFromFile(string filename)
        {
            var targetExtension = (new FileInfo(filename)).Extension.ToLower().Replace(".", "");
            var format = "";

            switch (targetExtension)
            {
                case "gpkg":
                    format = "GPKG";
                    break;
                case "shp":
                    format = "ESRI Shapefile";
                    break;
                case "kml":
                    format = "KML";
                    break;
                case "gpx":
                    format = "GPX";
                    break;
                case "dxf":
                    format = "DXF";
                    break;
                case "json":
                case "geojson":
                    format = "GeoJSON";
                    break;
                case "csv":
                    format = "CSV";
                    break;
                case "gml":
                    format = "GML";
                    break;
                case "fgb":
                    format = "FlatGeobuf";
                    break;
            }

            return format;
        }

        public static List<LayerItem> GetInformation(string shareFolder, string filename, List<int> checkForEpsgs = null)
        {
            var result = new List<LayerItem>();

            if (!File.Exists(filename))
            {
                return result;
            }

            GdalConfiguration.ConfigureOgr();

            var dataSource = Ogr.Open(filename, 0);
            var layerCount = dataSource.GetLayerCount();

            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                var layerDefinition = new LayerItem();

                var layer = dataSource.GetLayerByIndex(layerIndex);

                var layerSpatialRef = layer.GetSpatialRef();

                layerDefinition.Name = layer.GetName();
                layerDefinition.FeatureCount = layer.GetFeatureCount(1);
                layerDefinition.SpatialReferenceAvailable = layerSpatialRef != null;

                if (layerSpatialRef != null)
                {
                    layerDefinition.Epsg = System.Convert.ToInt32(layerSpatialRef.GetAuthorityCode(null));
                }

                var featureCount = layer.GetFeatureCount(1);

                var fields = layer.GetLayerDefn();

                for (int i = 0; i < fields.GetFieldCount(); i++)
                {
                    var field = fields.GetFieldDefn(i);
                    layerDefinition.Properties.Add(new Property() { Name = field.GetName(), Type = field.GetFieldTypeName(field.GetFieldType()) });
                }

                var maxExtent = new Extent(new double[] { double.MaxValue, double.MaxValue, double.MinValue, double.MinValue });

                for (int featureIndex = 0; featureIndex < featureCount; featureIndex++)
                {
                    var feature = layer.GetFeature(featureIndex);

                    if (feature == null)
                    {
                        continue;
                    }

                    var geometry = feature.GetGeometryRef();

                    var geometryType = geometry.GetGeometryType().ToString();

                    if (!layerDefinition.GeometryTypes.Contains(geometryType))
                    {
                        layerDefinition.GeometryTypes.Add(geometryType);
                        layerDefinition.GeometryTypesGdal.Add(geometry.GetGeometryType());
                    }

                    var envelope = new Envelope();
                    geometry.GetEnvelope(envelope);

                    var featureExtent = new Extent(envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY);

                    maxExtent = featureExtent.Join(maxExtent);
                }

                layerDefinition.Extent = maxExtent;

                if (checkForEpsgs != null && checkForEpsgs.Count > 0)
                {
                    foreach (var epsg in checkForEpsgs)
                    {
                        if (layerDefinition.OverlapSpatialRefs.ContainsKey(epsg))
                        {
                            continue;
                        }

                        var crsDefinition = GetCrsDefinition(shareFolder, epsg);

                        if (crsDefinition == null)
                        {
                            //log.Error($"crs definition for epsg {epsg} not found");
                            continue;
                        }

                        layerDefinition.OverlapSpatialRefs.Add(epsg, CheckForValidCrsRange(crsDefinition, maxExtent.Array));
                    }
                }

                result.Add(layerDefinition);
                layer.Dispose();
            }

            dataSource.Dispose();
            return result;
        }

        public static bool CheckForValidCrsRange(CrsDefinition crsDefinition, double[] extent)
        {
            if (crsDefinition == null)
            {
                return false;
            }

            var extentCrs = new Extent(crsDefinition.XMin, crsDefinition.YMin, crsDefinition.XMax, crsDefinition.YMax);
            var extentFeature = new Extent(extent[0], extent[1], extent[2], extent[3]);

            return extentCrs.Intersection(extentFeature);
        }

        public static CrsDefinition GetCrsDefinition(string shareFolder, int epsg)
        {
            var pathCrsFile = Path.Combine(shareFolder, "crsDefinitionList.json");

            if (!File.Exists(pathCrsFile))
            {
                //log.Error($"crs file not found: {pathCrsFile}");
                return null;
            }

            var crsDefinitionsString = File.ReadAllText(pathCrsFile);
            CrsDefinition crsDefinitionFound = null;

            try
            {
                var crsDefinitions = JsonConvert.DeserializeObject<List<CrsDefinition>>(crsDefinitionsString);
                crsDefinitionFound = crsDefinitions.FirstOrDefault(x => x.Epsg == epsg);
            }
            catch (Exception ex)
            {
                //log.Error($"DES error: {pathCrsFile}, {ex}");
                return null;
            }

            if (crsDefinitionFound == null)
            {
                //log.Warn($"projection not found: {epsg}");
                return null;
            }

            return crsDefinitionFound;
        }

        public static string[] GetGeoSpatialFilesFromDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return new string[] { };
            }

            var directoryInfo = new DirectoryInfo(directoryPath);

            FileInfo[] fileInfos = new string[] { "*.shp", "*.gpkg", "*.json", "*.geojson", "*.dxf", "*.kml", "*.gpx", "*.csv", "*.gml" }
                .SelectMany(i => directoryInfo.GetFiles(i, SearchOption.AllDirectories))
                .ToArray();

            return fileInfos.Select(x => x.FullName).ToArray();
        }

        public static string[] GetAllRelatedFiles(string filePath) //example *.shp
        {
            if (!File.Exists(filePath))
            {
                //log.Error($"file does not exist {filePath}");
                return new string[] { };
            }

            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Directory == null)
            {
                //log.Error($"invalid directory, value == null");
                return new string[] { };
            }

            var fileNameBody = fileInfo.Name.Replace(fileInfo.Extension, "");

            return fileInfo.Directory.GetFiles($"{fileNameBody}.*", SearchOption.AllDirectories).Select(x => x.FullName).ToArray();
        }

        public static string[] RenameAllRelatedFiles(string filePath, string filePathTarget) //example *.shp
        {
            var files = new List<string>();
            var relatedFiles = GetAllRelatedFiles(filePath);

            if (relatedFiles.Length == 0)
            {
                return files.ToArray();
            }

            var fileInfo = new FileInfo(filePathTarget);
            var fileNameBody = fileInfo.Name.Replace(fileInfo.Extension, "");

            if (fileInfo.Directory == null)
            {
                //log.Error($"invalid directory, {fileInfo.FullName}");
                return files.ToArray();
            }

            foreach (var file in relatedFiles.Select(x => new FileInfo(x)))
            {
                var newFile = Path.Combine(fileInfo.Directory.FullName, $"{fileNameBody}{file.Extension}");
                File.Copy(file.FullName, newFile, true);
                files.Add(newFile);
            }

            return files.ToArray();
        }
    }
}
