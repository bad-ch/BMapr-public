using OSGeo.MapServer;
using System.Drawing;

namespace BMapr.GDAL.WebApi.Services
{
    public class Mapserver
    {
        public mapObj Map { get; set; }
        public string CrsWkt { get; set; }

        private int _debugLevel { get; set; }

        //mapserver support the following types of geometries in the layer chart|circle|line|point|polygon|raster|query

        public enum LayerGeometryType
        {
            Point = 0,
            Polyline =1,
            Polygon = 2
        }

        //mapserver support the following types of connections contour|inline|ogr|oraclespatial|plugin|postgis|sde|union|uvraster|wfs|wms

        public enum LayerConnectionType
        {
            Inline = 0
        }

        private Dictionary<LayerGeometryType, MS_LAYER_TYPE> _geometryConverter;
        private Dictionary<LayerConnectionType, MS_CONNECTION_TYPE> _connectionConverter;

        public int DebugLevel
        {
            get
            {
                return Map.debug;
            }
        }

        public int LayerCount
        {
            get
            {
                return Map.numlayers;
            }
        }

        private void Ini()
        {
            _geometryConverter = new Dictionary<LayerGeometryType, MS_LAYER_TYPE>
            {
                {LayerGeometryType.Point, MS_LAYER_TYPE.MS_LAYER_POINT},
                {LayerGeometryType.Polyline, MS_LAYER_TYPE.MS_LAYER_LINE},
                {LayerGeometryType.Polygon, MS_LAYER_TYPE.MS_LAYER_POLYGON}
            };

            _connectionConverter = new Dictionary<LayerConnectionType, MS_CONNECTION_TYPE>
            {
                {LayerConnectionType.Inline, MS_CONNECTION_TYPE.MS_INLINE}
            };

        }

        public Mapserver()
        {
            Ini();
            //GdalConfiguration.ConfigureGdal();
            //GdalConfiguration.ConfigureOgr();
            Map = new mapObj("");

            _debugLevel = 1;

            Map.debug = _debugLevel;
            Map.units = OSGeo.MapServer.MS_UNITS.MS_METERS;
            var check = Map.setFontSet("C:\\Windows\\Fonts\\Arial.ttf");

            //Map.setProjection(CrsWkt);

            //Map.configoptions.set("PROJ_LIB", @"C:\Program Files\GDAL\projlib");
            //Map.configoptions.set("MS_ERRORFILE", @"C:\TEMP\mapserverxxx.log");
            Map.applyConfigOptions();
        }

        public Mapserver(FileInfo mapfile)
        {
            if (!File.Exists(mapfile.FullName))
            {
                return;
            }

            Ini();
            //GdalConfiguration.ConfigureGdal();
            //GdalConfiguration.ConfigureOgr();

            Map = new mapObj(mapfile.FullName);
        }

        public Mapserver(string mapContent)
        {
            Ini();
            //GdalConfiguration.ConfigureGdal();
            //GdalConfiguration.ConfigureOgr();

            Map = ConfigService.GetMapObject(mapContent);
        }

        public void AddGeometryToLayer(string layername, string wktGeometry, string value)
        {
            var layer = Map.getLayerByName(layername);

            if (layer == null)
            {
                return;
            }

            AddGeometryToLayer(layer.index, wktGeometry,value);
        }


        public void AddGeometryToLayer(int index, string wktGeometry, string value)
        {
            if (index >= Map.numlayers)
            {
                return;
            }

            var layer = Map.getLayer(index);
            var shape = shapeObj.fromWKT(wktGeometry);

            shape.initValues(1);
            shape.setValue(0, value);

            layer.addFeature(shape);
        }

        public void AddLayerByString(string layerDefinition)
        {
            var layer = new layerObj(Map);
            layer.updateFromString(layerDefinition);

            //layer.setProjection(wktProj); //TODO check necessity ??
        }

        public void AddLayer(string name, LayerConnectionType connectionType, LayerGeometryType geometryType)
        {

            var layer = new layerObj(Map);

            layer.connectiontype = _connectionConverter[connectionType];
            layer.type = _geometryConverter[geometryType];
            layer.name = name;
            layer.status = 1;
        }

        public void ApplySldDefinition(string layername, string sldDescription)
        {
            var layer = Map.getLayerByName(layername);

            if (layer == null)
            {
                return;
            }

            var i = layer.applySLD(sldDescription, null);
        }

        public void MapserverSetExtent(double xMin, double yMin, double xMax, double yMax, int width, int height)
        {
            Map.width = width;   //it is very important that the propertion from the extent and the picture is the same otherwise mapserver change the extent
            Map.height = height; //it is very important that the propertion from the extent and the picture is the same otherwise mapserver change the extent
            Map.setExtent(xMin, yMin, xMax, yMax);
        }

        public byte[] MapserverDrawImage(string mimeType, int width, int height)
        {
            outputFormatObj outputFormat = null;

            Map.width = width;
            Map.height = height;

            switch (mimeType.ToLower())
            {
                case "image/png":

                    outputFormat = new outputFormatObj("AGG/PNG", "png");
                    outputFormat.transparent = 1;
                    outputFormat.imagemode = Convert.ToInt32(OSGeo.MapServer.MS_IMAGEMODE.MS_IMAGEMODE_RGBA);

                    break;

                case "image/jpg":
                case "image/jpeg":

                    outputFormat = new outputFormatObj("AGG/JPEG", "jpg");
                    outputFormat.transparent = 1;
                    outputFormat.imagemode = Convert.ToInt32(OSGeo.MapServer.MS_IMAGEMODE.MS_IMAGEMODE_RGBA);

                    break;

                default:

                    return null;
            }

            Map.setOutputFormat(outputFormat);

            imageObj imageObj;

            try
            {
                imageObj = Map.draw();
            }
            catch (Exception ex)
            {
                return null;           
            }

            return imageObj.getBytes();
        }

        public Image MapserverDrawLegendImage(string layername, int width, int height)
        {
            var layer = Map.getLayerByName(layername);

            if (layer == null)
            {
                return null;
            }

            return MapserverDrawLegendImage(layer.index, width, height);
        }

        public Image MapserverDrawLegendImage(int index, int width, int height)
        {
            if (index >= Map.numlayers)
            {
                return null;
            }

            var layer = Map.getLayer(index);

            var image = layer.getClass(0).createLegendIcon(Map, layer, width, height);

            var memoryStream = new MemoryStream(image.getBytes());
            return Image.FromStream(memoryStream);
        }

        public string GetLayername(int index)
        {
            if (index >= Map.numlayers)
            {
                return null;
            }

            var layer = Map.getLayer(index);
            return layer.name;
        }

        public List<int> GetLayerindexes(string name)
        {
            var layers = new List<int>();

            if (String.IsNullOrEmpty(name))
            {
                return layers;
            }

            for (int i = 0; i <  Map.numlayers; i++)
            {
                var layer = Map.getLayer(i);

                if (layer.name == name)
                {
                    layers.Add(i);
                }
            }

            return layers;
        }

        public void SetAllLayers(bool visible)
        {
            for (int i = 0; i < Map.numlayers; i++)
            {
                var layer = Map.getLayer(i);
                layer.status = visible ? 1 : 0;                
            }
        }

        public bool SetLayerByName(string name, bool visible)
        {
            var layerIndexes = GetLayerindexes(name);

            if (layerIndexes.Count == 0)
            {
                return false;
            }

            foreach (var layerIndex in layerIndexes)
            {
                var layer = Map.getLayer(layerIndex);

                layer.status = visible ? 1 : 0;
            }

            return true;
        }

        public bool SetLayerById(int layerIndex, bool visible)
        {
            if (!(layerIndex >= 0 && layerIndex < Map.numlayers))
            {
                return false;
            }

            var layer = Map.getLayer(layerIndex);
            layer.status = visible ? 1 : 0;

            return true;
        }

        public Dictionary<string,Image> GetLegendAsImages(int width, int height, bool allLayer = true)
        {
            var legends = new Dictionary<string, Image>();

            for (int i = 0; i < Map.numlayers; i++)
            {
                var layer = Map.getLayer(i);

                if (layer.status == 0 && !allLayer) //ignore layers which are switched off in legend
                {
                    continue;
                }

                var layerName = string.IsNullOrEmpty(layer.name) ? i.ToString() : layer.name;

                for (int j = 0; j < layer.numclasses; j++)
                {
                    var classObj = layer.getClass(j);
                    var classname = string.IsNullOrEmpty(classObj.name) ? j.ToString() : classObj.name;
                    var image = classObj.createLegendIcon(Map, layer, width, height);
                    var legendLabel = $"{layerName}, {classname}";

                    var memoryStream = new MemoryStream(image.getBytes());
                    legends.Add(legendLabel, Image.FromStream(memoryStream));
                }
            }

            return legends; 
        }

        public Dictionary<string, string> GetLegendAsSld(bool allLayer = true)
        {
            var legends = new Dictionary<string, string>();

            for (int i = 0; i < Map.numlayers; i++)
            {
                var layer = Map.getLayer(i);

                if (layer.status == 0 && !allLayer) //ignore layers which are switched off in legend
                {
                    continue;
                }

                var layerName = string.IsNullOrEmpty(layer.name) ? i.ToString() : layer.name;
                var sld = layer.generateSLD();

                legends.Add(layerName, sld);
            }

            return legends;
        }
    }
}
