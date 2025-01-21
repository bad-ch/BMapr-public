using BMapr.GDAL.WebApi.Models;
using Microsoft.Extensions.Caching.Memory;
using OSGeo.MapServer;
using System.Reflection;

namespace BMapr.GDAL.WebApi.Services
{
    public class MapserverService
    {
        public mapObj Map { get; set; }
        public string _mapFilePath { get; set; }
        private void Ini()
        {
            //GdalConfiguration.ConfigureGdal();
            //GdalConfiguration.ConfigureOgr();
        }

        public MapserverService(FileInfo mapPath)
        {
            Ini();
            _mapFilePath = mapPath.FullName;
            Map = new mapObj(_mapFilePath);
        }

        public MapserverService(string mapContent)
        {
            Ini();
            Map = new mapObj(mapContent, 1);
        }

        public MapserverService(Config config, string project)
        {
            var mapMetadata = MapFileService.GetMapFromProject(project, config);

            if (!mapMetadata.Succesfully || string.IsNullOrEmpty(mapMetadata.Value.Key) || string.IsNullOrEmpty(mapMetadata.Value.FilePath))
            {
                throw new Exception("Metadata from map not created successfully");
            }

            var mapFile = MapFileService.GetMapPathFromCache(mapMetadata.Value.Key, mapMetadata.Value.FilePath, config, project) ?? string.Empty;

            Ini();

            try
            {
                Map = new mapObj(mapFile);
            }
            catch
            {
                throw new Exception("Error load map file");
            }
        }

        public new Dictionary<string, object> GetMetadata(object obj)
        {
            var result = new Dictionary<string, object>();

            Type t = obj.GetType();
            string fieldName;
            object propertyValue;

            foreach (PropertyInfo pi in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                fieldName = pi.Name;
                propertyValue = pi.GetValue(obj, null);

                if (propertyValue != null && obj.GetType() == typeof(OSGeo.MapServer.layerObj) && fieldName == "units")
                {
                    //fix wrong type
                    result.Add(fieldName, Units()[(int)propertyValue] );
                    continue;
                }
                if (propertyValue != null && obj.GetType() == typeof(OSGeo.MapServer.classObj) && propertyValue.GetType() == typeof(OSGeo.MapServer.layerObj)) //skip layer in class object
                {
                    continue;
                }


                if (propertyValue != null && propertyValue.GetType() == typeof(OSGeo.MapServer.hashTableObj))
                {
                    result.Add(fieldName, GetHashTable((OSGeo.MapServer.hashTableObj)propertyValue));
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(OSGeo.MapServer.clusterObj)) //layer
                {
                    result.Add(fieldName, propertyValue);
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(OSGeo.MapServer.mapObj))
                {
                    //ignore
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(OSGeo.MapServer.symbolSetObj))
                {
                    result.Add(fieldName, propertyValue);
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(OSGeo.MapServer.fontSetObj))
                {
                    result.Add(fieldName, propertyValue);
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(OSGeo.MapServer.labelCacheObj))
                {
                    result.Add(fieldName, propertyValue);
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(OSGeo.MapServer.outputFormatObj))
                {
                    result.Add(fieldName, propertyValue);
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(OSGeo.MapServer.referenceMapObj))
                {
                    result.Add(fieldName, propertyValue);
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(OSGeo.MapServer.scalebarObj))
                {
                    result.Add(fieldName, propertyValue);
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(OSGeo.MapServer.legendObj))
                {
                    result.Add(fieldName, GetMetadata(propertyValue));
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(OSGeo.MapServer.queryMapObj))
                {
                    result.Add(fieldName, GetMetadata(propertyValue));
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(OSGeo.MapServer.webObj))
                {
                    result.Add(fieldName, GetMetadata(propertyValue));
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(OSGeo.MapServer.rectObj))
                {
                    result.Add(fieldName, propertyValue);
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(OSGeo.MapServer.colorObj))
                {
                    result.Add(fieldName, propertyValue);
                }
                else if (propertyValue != null && propertyValue.GetType().BaseType == typeof(Enum))
                {
                    result.Add(fieldName, propertyValue.ToString());
                }
                else
                {
                    result.Add(fieldName, propertyValue);
                }
            }

            if (obj != null && obj.GetType() == typeof(OSGeo.MapServer.mapObj))
            {
                var map = (mapObj) obj;
                var layers = new List<object>();

                for (int i = 0; i < map.numlayers; i++)
                {
                    var layer = GetMetadata(map.getLayer(i));
                    layers.Add(layer);
                }

                result.Add("layers", layers);
            }

            if (obj != null && obj.GetType() == typeof(OSGeo.MapServer.layerObj))
            {
                var layer = (layerObj)obj;
                var classes = new List<object>();

                for (int i = 0; i < layer.numclasses; i++)
                {
                    var classItem = GetMetadata(layer.getClass(i));
                    classes.Add(classItem);
                }

                result.Add("classes", classes);
            }

            if (obj != null && obj.GetType() == typeof(OSGeo.MapServer.classObj))
            {
                var classItem = (classObj)obj;
                var styles = new List<object>();

                for (int i = 0; i < classItem.numstyles; i++)
                {
                    var styleItem = GetMetadata(classItem.getStyle(i));
                    styles.Add(styleItem);
                }

                result.Add("styles", styles);

                var labels = new List<object>();

                for (int i = 0; i < classItem.numlabels; i++)
                {
                    var labelItem = GetMetadata(classItem.getLabel(i));
                    styles.Add(labelItem);
                }

                result.Add("labels", labels);
            }

            return result;
        }

        #region private

        private Dictionary<string, string> GetHashTable(hashTableObj hashTable)
        {
            var content = new Dictionary<string, string>();
            string prevKey = null;

            for (int j = 0; j < hashTable.numitems; j++)
            {
                var key = hashTable.nextKey(prevKey);
                var item = hashTable.get(key, "");
                content.Add(key, item);
                prevKey = key;
            }

            return content;
        }

        private Dictionary<int, string> Units()
        {
            return new Dictionary<int, string>()
            {
                {(int)OSGeo.MapServer.MS_UNITS.MS_DD, "MS_DD"},
                {(int)OSGeo.MapServer.MS_UNITS.MS_FEET, "MS_FEET"},
                {(int)OSGeo.MapServer.MS_UNITS.MS_INCHES, "MS_INCHES"},
                {(int)OSGeo.MapServer.MS_UNITS.MS_KILOMETERS, "MS_KILOMETERS"},
                {(int)OSGeo.MapServer.MS_UNITS.MS_METERS, "MS_METERS"},
                {(int)OSGeo.MapServer.MS_UNITS.MS_MILES, "MS_MILES"},
                {(int)OSGeo.MapServer.MS_UNITS.MS_NAUTICALMILES, "MS_NAUTICALMILES"},
                {(int)OSGeo.MapServer.MS_UNITS.MS_PERCENTAGES, "MS_PERCENTAGES"},
                {(int)OSGeo.MapServer.MS_UNITS.MS_PIXELS, "MS_PIXELS"},
            };
        }

        #endregion
    }
}
