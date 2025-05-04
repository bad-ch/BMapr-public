using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.MapFile;
using BMapr.GDAL.WebApi.Models.OgcApi.Features;
using BMapr.GDAL.WebApi.Models.Spatial;
using Newtonsoft.Json;
using OSGeo.OGR;
using Extent = BMapr.GDAL.WebApi.Models.OgcApi.Features.Extent;
using Feature = OSGeo.OGR.Feature;
using FeatureCollection = BMapr.GDAL.WebApi.Models.OgcApi.Features.FeatureCollection;
using Geometry = OSGeo.OGR.Geometry;

namespace BMapr.GDAL.WebApi.Services
{
    public static class OgcApiFeaturesService
    {
        public static readonly string JsonMimeType = "application/json";

        public static Result<Collections> GetCollections(Config config, List<CrsDefinition> crsDefinitions, string project, Collections collections, string urlCollections)
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
                var resultCollection = GetCollectionItem(crsDefinitions, mapFile, item, urlCollections);
                collections.CollectionList.Add(resultCollection.Value);
            });

            return new Result<Collections>(){Value = collections, Succesfully = true};
        }

        public static Result<Collection> GetCollection(Config config, List<CrsDefinition> crsDefinitions, string project, string collectionId, string urlCollections)
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

                var resultCollection = GetCollectionItem(crsDefinitions, mapFile, item, urlCollections);
                collection = resultCollection.Value;
            });

            return new Result<Collection>() { Value = collection, Succesfully = true };
        }

        private static Result<Collection> GetCollectionItem(List<CrsDefinition> crsDefinitions, MapFile mapFile, Models.MapFile.Layer item, string urlCollections)
        {
            var collection = new Collection()
            {
                Id = item.Name,
                Title = item.Name, // todo introduce metadata tag
                Description = item.Name, // todo introduce metadata tag
                Extent = new Extent()
                {
                    Temporal = new Temporal(),
                    Spatial = new Spatial()
                    {
                        Crs = "http://www.opengis.net/def/crs/OGC/1.3/CRS84"
                    }
                }
            };

            collection.StorageCrs = "http://www.opengis.net/def/crs/OGC/1.3/CRS84"; //$"http://www.opengis.net/def/crs/EPSG/0/{item.Metadata.MshEPSG}";
            collection.Crs.Add("http://www.opengis.net/def/crs/OGC/1.3/CRS84");

            var listMap = GetCrsList(mapFile.Web.Metadata.WfsSrs);

            if (listMap.Count > 0)
            {
                collection.Crs.AddRange(listMap);
            }

            var listLayer = GetCrsList(item.Metadata.WfsSrs);

            if (listLayer.Count > 0)
            {
                collection.Crs.AddRange(listLayer);
            }

            collection.Crs = collection.Crs.Distinct().ToList();

            if (item.Extent.Minx > 0)
            {
                collection.Extent.Spatial.Bbox.Add(new List<double>() { item.Extent.Minx, item.Extent.Miny, item.Extent.Maxx, item.Extent.Maxy });
            }
            else
            {
                var crsDefinition = crsDefinitions.First(x => x.Epsg.ToString() == item.Metadata.MshEPSG);

                collection.Extent.Spatial.Bbox.Add(new List<double>() { crsDefinition.WestBoundLon, crsDefinition.SouthBoundLat, crsDefinition.EastBoundLon, crsDefinition.NorthBoundLat });
            }

            collection.Links.Add(new Link() { Rel = "self", Title = "This collection", Type = "application/json", Href = $"{urlCollections}/{item.Name}" });
            collection.Links.Add(new Link() { Rel = "items", Title = $"{item.Name} as GeoJSON", Type = "application/geo+json", Href = $"{urlCollections}/{item.Name}/items?f=geojson" });
            //collection.Links.Add(new Link() { Rel = "describedby", Title = $"Schema for {item.Name}", Type = "application/json", Href = $"{urlCollections}/{item.Name}/schema?f=application/json" });

            return new Result<Collection>(){Value = collection, Succesfully = true};
        }

        // todo return value
        public static Result<FeatureCollection> GetItems(Config config, string project, string collectionId, List<double> bbox, string? bboxCrs, string? crs, string query, int? offset, int? limit, string f)
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
            var result = new Result<FeatureCollection>();
            int crsOut = 0;
            int crsBboxOut = 0;

            featureCollection.Name = collectionId;

            if (string.IsNullOrEmpty(crs))
            {
                if (!string.IsNullOrEmpty(bboxCrs))
                {
                    result.AddMessage("Takeover CRS from bbox");
                    featureCollection.Crs = bboxCrs;
                    crsOut = ProjectionService.getEPSGCode(bboxCrs);
                }
                else
                {
                    result.AddMessage("Set CRS as default to WGS84");
                    featureCollection.Crs = "http://www.opengis.net/def/crs/OGC/1.3/CRS84";
                    crsOut = 4326;
                }
            }
            else
            {
                result.AddMessage("Set CRS from request");
                featureCollection.Crs = crs;
                crsOut = ProjectionService.getEPSGCode(crs);
            }

            if (string.IsNullOrEmpty(bboxCrs))
            {
                result.AddMessage("Set BBox-CRS from CRS");
                crsBboxOut = crsOut;
            }
            else
            {
                result.AddMessage("Set BBox-CRS from request");
                crsBboxOut = ProjectionService.getEPSGCode(bboxCrs);
            }

            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                //if (layer.GetName() != collectionId)
                //{
                //    continue;
                //}

                var layer = dataSource.GetLayerByIndex(layerIndex);
                var featureCount = layer.GetFeatureCount(1);
                result.Messages.Add($"feature count {featureCount}");

                var spatialRef = layer.GetSpatialRef();
                result.Messages.Add($"layer spatial ref name {spatialRef.GetName()}, {spatialRef.GetAuthorityCode("PROJCS")}");

                var authorityCode = spatialRef.GetAuthorityCode("PROJCS");

                int crsSrc = 0;

                if (!string.IsNullOrEmpty(authorityCode))
                {
                    crsSrc = Convert.ToInt32(authorityCode);
                }
                else if (layerConfig.EPSG > 0)
                {
                    crsSrc = layerConfig.EPSG;
                }
                else
                {
                    throw new Exception("OGC API featurues, data has no CRS");
                }

                //Envelope layerExtent = null!;
                //var extentStatus = layer.GetExtent(layerExtent, 1);

                Geometry geometryBbox = null;

                if (bbox.Count > 0)
                {
                    // todo bbox-crs

                    if (bbox.Count == 4)
                    {
                        if (crsBboxOut == 4326)
                        {
                            geometryBbox = Geometry.CreateFromWkt($"POLYGON(({bbox[1]} {bbox[0]}, {bbox[1]} {bbox[2]},{bbox[3]} {bbox[2]}, {bbox[3]} {bbox[0]}, {bbox[1]} {bbox[0]}))");
                        }
                        else
                        {
                            geometryBbox = Geometry.CreateFromWkt($"POLYGON(({bbox[0]} {bbox[1]}, {bbox[2]} {bbox[1]},{bbox[2]} {bbox[3]}, {bbox[0]} {bbox[3]}, {bbox[0]} {bbox[1]}))");
                        }

                        if (crsBboxOut != crsSrc)
                        {
                            var outBboxCrs = new OSGeo.OSR.SpatialReference("");
                            outBboxCrs.ImportFromEPSG(crsBboxOut);
                            //var t1 = outCrs.GetAxisOrientation(null, 0);
                            //var t2 = outCrs.GetAxisOrientation(null, 1);


                            var srcCrs = new OSGeo.OSR.SpatialReference("");
                            srcCrs.ImportFromEPSG(crsSrc);
                            //var t3 = srcCrs.GetAxisOrientation(null, 0);
                            //var t4 = srcCrs.GetAxisOrientation(null, 1);

                            var coordTransBbox = new OSGeo.OSR.CoordinateTransformation(outBboxCrs, srcCrs);

                            geometryBbox.Transform(coordTransBbox);
                        }

                        if (geometryBbox == null)
                        {
                            throw new Exception("OGC API featurues, bbox geometry wrong");
                        }

                        var wktBBoxReproj = GeometryService.GetStringFromOgrGeometry(geometryBbox, "wkt");

                        layer.SetSpatialFilter(geometryBbox);
                    }
                    else if (bbox.Count == 6)
                    {
                        throw new NotImplementedException("bbox with 6 values not implemented");
                        layer.SetSpatialFilter(Geometry.CreateFromWkt($"POLYGON(({bbox[0]} {bbox[1]}, {bbox[3]} {bbox[1]},{bbox[3]} {bbox[4]}, {bbox[0]} {bbox[4]}, {bbox[0]} {bbox[1]}))"));
                    }
                    else
                    {
                        result.Messages.Add("Ignore spatial filter because bbox is invalid");
                    }
                }

                long featureCountFiltered;

                if (!string.IsNullOrEmpty(query))
                {
                    layer.SetAttributeFilter(query); //$"{featureList.IdFieldName} IN ({string.Join(',', featureList.Bodies.Select(x => $"'{x.Id}'"))})"

                    featureCountFiltered = layer.GetFeatureCount(1);

                }
                else
                {
                    featureCountFiltered = layer.GetFeatureCount(1) - 1;
                }

                featureCollection.NumberMatched = featureCountFiltered;

                result.Messages.Add($"feature count with filter {featureCountFiltered}");

                // force paging

                if (offset == null && limit != null && limit < featureCountFiltered)
                {
                    // todo log
                    offset = 0;
                }

                if (offset != null && limit != null)
                {
                    var next = GetNavigationLink(true, config, project, collectionId, bbox, bboxCrs, query, (int)offset, (int)limit, f, (int)featureCountFiltered);
                    var prev = GetNavigationLink(false, config, project, collectionId, bbox, bboxCrs, query, (int)offset, (int)limit, f, (int)featureCountFiltered);

                    if (next != null)
                    {
                        featureCollection.Links.Add(next);
                    }

                    if (prev != null)
                    {
                        featureCollection.Links.Add(prev);
                    }
                }

                Feature feature;
                long i = 0;
                long j = 0;
                long limitCount = 0;
                layer.ResetReading();

                while ((feature = layer.GetNextFeature()) != null)
                {
                    i++;

                    if (offset != null && i <= offset)
                    {
                        continue;
                    }

                    if (limit != 0 && limitCount >= limit)
                    {
                        continue;
                    }

                    if (limit != null)
                    {
                        limitCount++;
                    }

                    var featureCls = new Models.OgcApi.Features.Feature() { Type = "Feature" };
                    var geometry = feature.GetGeometryRef();

                    if (geometry == null)
                    {
                        //todo log
                        continue;
                    }

                    // *********************************************************************************************************************
                    // reproject data

                    if (crsOut != crsSrc)
                    {
                        var srcCrs = new OSGeo.OSR.SpatialReference("");
                        srcCrs.ImportFromEPSG(crsSrc);

                        var outCrs = new OSGeo.OSR.SpatialReference("");
                        outCrs.ImportFromEPSG(crsOut);

                        var coordTrans = new OSGeo.OSR.CoordinateTransformation(srcCrs, outCrs);

                        geometry.Transform(coordTrans);
                    }

                    featureCls.Id = $"{feature.GetFID()}";

                    var geometryGeoJson = GeometryService.GetStringFromOgrGeometry(geometry, "geojson", false);

                    if (!string.IsNullOrEmpty(geometryGeoJson))
                    {
                        j++;
                        featureCls.Properties = GetFeatureProperties(feature);
                        featureCls.Geometry = JsonConvert.DeserializeObject<dynamic>(geometryGeoJson)!;
                        featureCollection.Features.Add(featureCls);
                    }
                }

                featureCollection.NumberReturned = j;

                layer.Dispose();
            }

            dataSource.Dispose();

            result.Value = featureCollection;
            result.Succesfully = true;

            return result;
        }

        public static Result<Schema> GetQueryables(Config config, string project, string collectionId, string f)
        {
            var mapMetadata = MapFileService.GetMapFromProject(project, config);

            if (!mapMetadata.Succesfully || string.IsNullOrEmpty(mapMetadata.Value.Key) || string.IsNullOrEmpty(mapMetadata.Value.FilePath))
            {
                return new Result<Schema>() { Value = null, Succesfully = false, Messages = new List<string>() { "Error getting map metadata" } };
            }

            var resultMapConfig = MapFileService.GetMapConfigFromCache(mapMetadata.Value.Key, mapMetadata.Value.FilePath, config, project);

            if (!resultMapConfig.Succesfully)
            {
                return new Result<Schema>() { Value = null, Succesfully = false, Messages = new List<string>() { "Config map not opened successfully" } };
            }

            var mapConfig = resultMapConfig.Value;
            var layerConfig = mapConfig.GetLayerConfig(collectionId);

            GdalConfiguration.ConfigureOgr();

            var dataSource = Ogr.Open(layerConfig.Connection, 0);
            var layerCount = dataSource.GetLayerCount();
            var schema = new Schema();

            schema.Title = collectionId;

            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                var layer = dataSource.GetLayerByIndex(layerIndex);

                if (layerIndex > 0)
                {
                    continue; // skip other layers than index 0
                }

                if (layer != null)
                {
                    var layerDefinition = layer.GetLayerDefn();

                    for (int fgi = 0; fgi < layerDefinition.GetGeomFieldCount(); fgi++)
                    {
                        var geometryFieldDef = layerDefinition.GetGeomFieldDefn(fgi);
                        var name = geometryFieldDef.GetName();

                        schema.Properties.Add(name,new SchemaProperty()
                        {
                            Title = name,
                            Role = fgi == 0 ? "primary-geometry" : "secondary-geometry",
                            Format = GetGeometryFormat(geometryFieldDef),
                            Ref = "https://geojson.org/schema/Geometry.json"
                        });
                    }

                    for (int fi = 0; fi < layerDefinition.GetFieldCount(); fi++)
                    {
                        var fieldDef = layer.GetLayerDefn().GetFieldDefn(fi);
                        var name = fieldDef.GetName();
                        var nameLc = name.ToLower();
                        var format = GetFieldFormat(fieldDef);

                        schema.Properties.Add(name, new SchemaProperty()
                        {
                            Title = name,
                            Role = (nameLc == "id" || nameLc == "fid" || nameLc == "gid") ? "id" : null,
                            Format = format.Item1,
                            //Enum = format.Item2 not supported yet
                        });
                    }
                }
            }

            schema.Id = $"{config.Host}/api/ogcapi/features/{project}/collections/{collectionId}/queryables?f=json";

            return new Result<Schema>(){Value = schema};
        }

        private static string GetGeometryFormat(GeomFieldDefn geometryFieldDef)
        {
            var type = geometryFieldDef.GetFieldType();

            switch (type)
            {
                case wkbGeometryType.wkbPoint:
                case wkbGeometryType.wkbPoint25D:
                case wkbGeometryType.wkbPointM:
                case wkbGeometryType.wkbPointZM:
                    return "geometry-point";
                case wkbGeometryType.wkbLineString:
                case wkbGeometryType.wkbLineString25D:
                case wkbGeometryType.wkbLineStringM:
                case wkbGeometryType.wkbLineStringZM:
                case wkbGeometryType.wkbCurve:
                case wkbGeometryType.wkbCurveM:
                case wkbGeometryType.wkbCurveZM:
                    return "geometry-linestring";
                case wkbGeometryType.wkbPolygon:
                case wkbGeometryType.wkbPolygon25D:
                case wkbGeometryType.wkbPolygonM:
                case wkbGeometryType.wkbPolygonZM:
                case wkbGeometryType.wkbCurvePolygon:
                case wkbGeometryType.wkbCurvePolygonM:
                case wkbGeometryType.wkbCurvePolygonZM:
                    return "geometry-polygon";
                default:
                    return $"not supported format {type.ToString()}";
            }
        }

        private static (string, string) GetFieldFormat(FieldDefn fieldDef)
        {
            var type = fieldDef.GetFieldType();

            switch (type)
            {
                case FieldType.OFTInteger:
                case FieldType.OFTInteger64:
                    return ("integer", string.Empty);
                case FieldType.OFTIntegerList:
                case FieldType.OFTInteger64List:
                    return ("integer", "enum");
                case FieldType.OFTReal:
                    return ("number", string.Empty);
                case FieldType.OFTRealList:
                    return ("number", "enum");
                case FieldType.OFTString:
                case FieldType.OFTWideString:
                    return ("string", string.Empty);
                case FieldType.OFTStringList:
                case FieldType.OFTWideStringList:
                    return ("string", "enum");
                case FieldType.OFTDate:
                    return ("date", string.Empty);
                case FieldType.OFTTime:
                    return ("time", string.Empty);
                case FieldType.OFTDateTime:
                    return ("date-time", string.Empty);
                default:
                    return ($"not supported format {type.ToString()}", string.Empty);
            }
        }

        private static Dictionary<string,object> GetFeatureProperties(Feature feature)
        {
            Dictionary<string, object> properties = new();

            var featureDef = feature.GetDefnRef();
            var fieldCount = featureDef.GetFieldCount();

            for (int i = 0; i < fieldCount; i++)
            {
                var field = featureDef.GetFieldDefn(i);
                var fieldType = field.GetFieldType();

                switch (fieldType)
                {
                    case FieldType.OFTInteger:
                        properties.Add(field.GetName(), feature.GetFieldAsInteger(i));
                        break;
                    case FieldType.OFTIntegerList:
                        properties.Add(field.GetName(), feature.GetFieldAsIntegerList(i, out int count));
                        break;
                    case FieldType.OFTReal:
                        properties.Add(field.GetName(), feature.GetFieldAsDouble(i));
                        break;
                    case FieldType.OFTRealList:
                        properties.Add(field.GetName(), feature.GetFieldAsDoubleList(i, out int count2));
                        break;
                    case FieldType.OFTString:
                        properties.Add(field.GetName(), feature.GetFieldAsString(i));
                        break;
                    case FieldType.OFTStringList:
                        properties.Add(field.GetName(), feature.GetFieldAsStringList(i));
                        break;
                    case FieldType.OFTWideString:
                        properties.Add(field.GetName(), feature.GetFieldAsString(i));
                        break;
                    case FieldType.OFTWideStringList:
                        properties.Add(field.GetName(), feature.GetFieldAsStringList(i));
                        break;
                    case FieldType.OFTBinary:
                        // no support
                        break;
                    case FieldType.OFTDate:
                        properties.Add(field.GetName(), feature.GetFieldAsISO8601DateTime(i, []));
                        break;
                    case FieldType.OFTTime:
                        properties.Add(field.GetName(), feature.GetFieldAsISO8601DateTime(i, []));
                        break;
                    case FieldType.OFTDateTime:
                        properties.Add(field.GetName(), feature.GetFieldAsISO8601DateTime(i, []));
                        break;
                    case FieldType.OFTInteger64:
                        properties.Add(field.GetName(), feature.GetFieldAsInteger64(i));
                        break;
                    case FieldType.OFTInteger64List:
                        // no support
                        break;
                }
            }

            return properties;
        }

        private static Link? GetNavigationLink(bool next, Config config, string project, string collectionId, List<double> bbox, string? bboxCrs, string query, int offset, int limit, string f, int maxCount) 
        {
            var urlCollections = $"{config.Host}/api/ogcapi/features/{project}/collections/{collectionId}/items?";

            bool flag=false;

            if (!string.IsNullOrEmpty(f))
            {
                urlCollections += $"f={f}";
                flag = true;
            }

            if (bbox.Count > 0)
            {
                urlCollections += $"{(flag?"&":"")}bbox={string.Join(',',bbox)}";
                flag = true;
            }

            if (!string.IsNullOrEmpty(bboxCrs))
            {
                urlCollections += $"{(flag ? "&" : "")}bbox-crs={bboxCrs}";
                flag = true;
            }

            if (!string.IsNullOrEmpty(query))
            {
                urlCollections += $"{(flag ? "&" : "")}query={query}";
                flag = true;
            }

            int offsetNew = 0;

            if (next)
            {
                offsetNew = offset + limit;
                if (offsetNew > maxCount)
                {
                    return null;
                }
            }
            else
            {
                offsetNew = offset - limit;
                if (offsetNew < 0)
                {
                    return null;
                }
            }

            urlCollections += $"{(flag ? "&" : "")}offset={(offsetNew)}";
            urlCollections += $"&limit={(limit)}";

            return new Link()
            {
                Href = urlCollections, Rel = (next ? "next" : "prev"), Title = $"{(next ? "Next" : "Previous")} page",
                Type = JsonMimeType
            };
        }

        private static List<string> GetCrsList(string listString)
        {
            List<string> result = new();
            var crsList = listString.ToLower().Replace("epsg:", "").Split(" ").Select(x => $"http://www.opengis.net/def/crs/EPSG/0/{x}").ToList();

            if (crsList.Count > 0)
            {
                result.AddRange(crsList);
            }

            return result.Distinct().ToList();
        }
    }
}
