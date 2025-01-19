using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.MapFile;
using BMapr.GDAL.WebApi.Models.OgcApi.Features;
using Newtonsoft.Json;
using OSGeo.OGR;
using Extent = BMapr.GDAL.WebApi.Models.OgcApi.Features.Extent;
using Feature = OSGeo.OGR.Feature;

namespace BMapr.GDAL.WebApi.Services
{
    public static class OgcApiFeaturesService
    {
        public static Result<Models.OgcApi.Features.Collections> GetToc(Config config, string project, Collections collections, string urlCollections)
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

            mapFile?.Layers.ForEach(item =>
            {
                var resultCollection = GetCollectionItem(mapFile, item, urlCollections);
                collections.CollectionList.Add(resultCollection.Value);
            });

            return new Result<Collections>(){Value = collections, Succesfully = true};
        }

        public static Result<Models.OgcApi.Features.Collection> GetCollection(Config config, string project, string collectionId, string urlCollections)
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
            var collection = new Collection();

            mapFile?.Layers.ForEach(item =>
            {
                if (item.Name != collectionId)
                {
                    return;
                }

                var resultCollection = GetCollectionItem(mapFile, item, urlCollections);
                collection = resultCollection.Value;
            });

            return new Result<Collection>() { Value = collection, Succesfully = true };
        }

        private static Result<Models.OgcApi.Features.Collection> GetCollectionItem(MapFile mapFile, Models.MapFile.Layer item, string urlCollections)
        {
            var collection = new Collection()
            {
                Id = item.Name,
                Title = item.Name, // todo introduce metadata tag
                Extent = new Extent()
                {
                    Temporal = new Temporal(),
                    Spatial = new Spatial()
                    {
                        Crs = "http://www.opengis.net/def/crs/OGC/1.3/CRS84" //$"http://www.opengis.net/def/crs/EPSG/0/{item.Metadata.MshEPSG}" // todo projection is not available ??
                    }
                }
            };

            collection.StorageCrs = "http://www.opengis.net/def/crs/OGC/1.3/CRS84"; //$"http://www.opengis.net/def/crs/EPSG/0/{item.Metadata.MshEPSG}";
            collection.Crs.Add("http://www.opengis.net/def/crs/OGC/1.3/CRS84"); //$"http://www.opengis.net/def/crs/EPSG/0/{item.Metadata.MshEPSG}");

            if (item.Extent.Minx > 0)
            {
                collection.Extent.Spatial.Bbox.Add(new List<double>() { 5.96, 45.82, 10.49, 47.81}); //item.Extent.Minx, item.Extent.Miny, item.Extent.Maxx, item.Extent.Maxy });
            }
            else
            {
                collection.Extent.Spatial.Bbox.Add(new List<double>() {5.96, 45.82, 10.49, 47.81}); //mapFile.Extent.Minx, mapFile.Extent.Miny, mapFile.Extent.Maxx, mapFile.Extent.Maxy });
            }

            collection.Links.Add(new Link() { Rel = "self", Title = "This collection", Type = "application/json", Href = $"{urlCollections}/{item.Name}" });
            collection.Links.Add(new Link() { Rel = "items", Title = $"{item.Name} as GeoJSON", Type = "application/geo+json", Href = $"{urlCollections}/{item.Name}/items?f=geojson" });
            //collection.Links.Add(new Link() { Rel = "describedby", Title = $"Schema for {item.Name}", Type = "application/json", Href = $"{urlCollections}/{item.Name}/schema?f=application/json" });

            return new Result<Collection>(){Value = collection, Succesfully = true};
        }

        // todo return value
        public static Result<FeatureCollection> GetItems(Config config, string project, string collectionId, List<double> bbox, string? bboxCrs, string query, int? offset, int? limit, string f)
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
            var featureCollection = new FeatureCollection() { Type = "FeatureCollection" };

            featureCollection.Name = collectionId;
            featureCollection.Crs = "http://www.opengis.net/def/crs/OGC/1.3/CRS84"; //"http://www.opengis.net/def/crs/EPSG/0/2056";

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
                long i = 0;

                do
                {
                    feature = layer.GetNextFeature();

                    if (i > 100)
                    {
                        continue;
                    }

                    if (feature != null)
                    {
                        var featureCls = new Models.OgcApi.Features.Feature() { Type = "Feature" };

                        var geometry = feature.GetGeometryRef();

                        var sourceCrs = new OSGeo.OSR.SpatialReference("");
                        sourceCrs.ImportFromEPSG(2056);

                        var targetCrs = new OSGeo.OSR.SpatialReference("");
                        targetCrs.ImportFromEPSG(4326);

                        var coordTrans = new OSGeo.OSR.CoordinateTransformation(sourceCrs, targetCrs);

                        geometry.Transform(coordTrans);

                        if (geometry == null)
                        {
                            //todo log
                            continue;
                        }

                        i++;
                        featureCls.Id = $"{i}";

                        var geometryGeoJson = GeometryService.GetStringFromOgrGeometry(geometry, "geojson", false);

                        if (!string.IsNullOrEmpty(geometryGeoJson))
                        {
                            featureCls.Geometry = JsonConvert.DeserializeObject<dynamic>(geometryGeoJson)!;
                            featureCollection.Features.Add(featureCls);
                        }

                    }

                } while (feature != null);

                featureCollection.NumberReturned = i;

                layer.Dispose();
            }

            dataSource.Dispose();
            // todo return value

            return new Result<FeatureCollection>(){Value = featureCollection, Succesfully = true};
        }
    }
}
