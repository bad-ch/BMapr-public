using Acornima.Ast;
using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.Spatial;
using BMapr.GDAL.WebApi.Models.Spatial.Vector;
using BMapr.GDAL.WebApi.Models.Tracking;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OSGeo.OGR;
using Feature = OSGeo.OGR.Feature;

namespace BMapr.GDAL.WebApi.Services
{
    public static class OgcApiFeaturesService
    {
        // todo return value
        public static void Get(Config config, string project, string collectionId, List<double> bbox, string query, int limit, string f)
        {
            var mapMetadata = MapFileService.GetMapFromProject(project, config);

            if (!mapMetadata.Succesfully || string.IsNullOrEmpty(mapMetadata.Value.Key) || string.IsNullOrEmpty(mapMetadata.Value.FilePath))
            {
                //_logger.LogError("error getting map metadata");
                return;// new BadRequestResult();
            }

            var resultMapConfig = MapFileService.GetMapConfigFromCache(mapMetadata.Value.Key, mapMetadata.Value.FilePath, config, project);

            if (!resultMapConfig.Succesfully)
            {
                //_logger.LogError("error getting map config");
                return; // new BadRequestResult();
            }

            var mapConfig = resultMapConfig.Value;
            var layerConfig = mapConfig.GetLayerConfig(collectionId);

            GdalConfiguration.ConfigureOgr();

            var dataSource = Ogr.Open(layerConfig.Connection, 0);
            var layerCount = dataSource.GetLayerCount();
            var featureCollection = new FeatureCollection() { Type = "FeatureCollection" };

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
                        var featureCls = new Models.Spatial.Vector.Feature() { Type = "Feature" };

                        var geometry = feature.GetGeometryRef();
                        var geometryGeoJson = GeometryService.GetStringFromOgrGeometry(geometry, "geojson", false);

                        if (!string.IsNullOrEmpty(geometryGeoJson))
                        {
                            featureCls.Geometry = new Models.Spatial.Vector.Geometry() { Type = "todo" };

                            featureCls.Geometry.Coordinates = JsonConvert.DeserializeObject<dynamic>(geometryGeoJson)!;


                            featureCollection.Features.Add(featureCls);
                        }

                    }

                } while (feature != null);

                layer.Dispose();
            }

            dataSource.Dispose();
            // todo return value

            return; //new OkObjectResult(null);
        }
    }
}
