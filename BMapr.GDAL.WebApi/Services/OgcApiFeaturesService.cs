using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.MapFile;
using BMapr.GDAL.WebApi.Models.Spatial.Vector2;
using Newtonsoft.Json;
using OSGeo.OGR;
using Feature = OSGeo.OGR.Feature;

namespace BMapr.GDAL.WebApi.Services
{
    public static class OgcApiFeaturesService
    {
        public static Result<Models.OgcApi.Features.Collections> GetToc(Config config, string project)
        {
            var mapserverService = new MapserverService(config, project);
            var result = mapserverService.GetMetadata(mapserverService.Map);
            var content = JsonConvert.SerializeObject(
                result,
                Formatting.None,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }
            );

            var mapFile = JsonConvert.DeserializeObject<MapFile>(content);

            return null;
        }

        // todo return value
        public static Result<Models.Spatial.Vector2.FeatureCollection> Get(Config config, string project, string collectionId, List<double> bbox, string query, int limit, string f)
        {
            var mapMetadata = MapFileService.GetMapFromProject(project, config);

            if (!mapMetadata.Succesfully || string.IsNullOrEmpty(mapMetadata.Value.Key) || string.IsNullOrEmpty(mapMetadata.Value.FilePath))
            {
                return new Result<FeatureCollection>() { Value = null, Succesfully = false, Messages = new List<string>() { "Error getting map metadata" } };
            }

            var resultMapConfig = MapFileService.GetMapConfigFromCache(mapMetadata.Value.Key, mapMetadata.Value.FilePath, config, project);

            if (!resultMapConfig.Succesfully)
            {
                return new Result<FeatureCollection>(){Value = null, Succesfully = false, Messages = new List<string>(){"Config map not opened successfully"}};
            }

            var mapConfig = resultMapConfig.Value;
            var layerConfig = mapConfig.GetLayerConfig(collectionId);

            GdalConfiguration.ConfigureOgr();

            var dataSource = Ogr.Open(layerConfig.Connection, 0);
            var layerCount = dataSource.GetLayerCount();
            var featureCollection = new Models.Spatial.Vector2.FeatureCollection() { Type = "FeatureCollection" };

            featureCollection.Name = collectionId;
            featureCollection.Crs = new {Type = "name", Properties = new {Name = "urn:ogc:def:crs:EPSG::2056"}};

            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                var layer = dataSource.GetLayerByIndex(layerIndex);

                //if (layer.GetName() != collectionId)
                //{
                //    // skip layer
                //    continue;
                //}

                if (bbox.Count > 0)
                {
                    //layer.SetSpatialFilter();
                }

                if (!string.IsNullOrEmpty(query))
                {
                    //layer.SetAttributeFilter($"{featureList.IdFieldName} IN ({string.Join(',', featureList.Bodies.Select(x => $"'{x.Id}'"))})");
                }

                var featureCountFiltered = layer.GetFeatureCount(1);

                Feature feature;

                do
                {
                    feature = layer.GetNextFeature();

                    if (feature != null)
                    {
                        var featureCls = new Models.Spatial.Vector2.Feature() { Type = "Feature" };

                        var geometry = feature.GetGeometryRef();

                        if (geometry == null)
                        {
                            //todo log
                            continue;
                        }

                        var geometryGeoJson = GeometryService.GetStringFromOgrGeometry(geometry, "geojson", false);

                        if (!string.IsNullOrEmpty(geometryGeoJson))
                        {
                            featureCls.Geometry = JsonConvert.DeserializeObject<dynamic>(geometryGeoJson)!;
                            featureCollection.Features.Add(featureCls);
                        }

                    }

                } while (feature != null);

                layer.Dispose();
            }

            dataSource.Dispose();
            // todo return value

            return new Result<FeatureCollection>(){Value = featureCollection, Succesfully = true};
        }
    }
}
