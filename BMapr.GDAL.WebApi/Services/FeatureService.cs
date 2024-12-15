using BMapr.GDAL.WebApi.Models.Spatial;
using OSGeo.OGR;

namespace BMapr.GDAL.WebApi.Services
{
    /// <summary>
    /// CRUD for OGR datasources
    /// </summary>
    public class FeatureService
    {
        public static void Insert(string connection, string layerName, FeatureList featureList)
        {
            GdalConfiguration.ConfigureOgr();

            var dataSource = Ogr.Open(connection, 1);
            var layerCount = dataSource.GetLayerCount();

            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                var layer = dataSource.GetLayerByIndex(layerIndex);

                //if (layer.GetName() != layerName)
                //{
                //    continue;
                //}

                layer.StartTransaction();

                foreach (var featureBody in featureList.Bodies)
                {
                    var newFeature = new Feature(layer.GetLayerDefn());
                    UpdateFeature(ref newFeature, featureBody);
                    layer.CreateFeature(newFeature);
                    
                    //var autoId = newFeature.GetFID();
                }

                layer.CommitTransaction();
                layer.Dispose();
            }

            dataSource.Dispose();
        }

        public static void Update(string connection, string layerName, FeatureList featureList)
        {
            GdalConfiguration.ConfigureOgr();

            var dataSource = Ogr.Open(connection, 1);
            var layerCount = dataSource.GetLayerCount();

            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                var layer = dataSource.GetLayerByIndex(layerIndex);

                //if (layer.GetName() != layerName)
                //{
                //    continue;
                //}

                var featureCount = layer.GetFeatureCount(1);

                var idIsString = IsString(layer, featureList.IdFieldName);

                if (idIsString == null)
                {
                    throw new Exception("FeatureService id column has not supported field type (string or number)");
                }

                if ((bool)idIsString)
                {
                    layer.SetAttributeFilter($"{featureList.IdFieldName} IN ({string.Join(',', featureList.Bodies.Select(x => $"'{x.Id}'"))})");
                }
                else
                {
                    layer.SetAttributeFilter($"{featureList.IdFieldName} IN ({string.Join(',', featureList.Bodies.Select(x => x.Id.ToString()))})");
                }

                var featureCountFiltered = layer.GetFeatureCount(1);

                layer.StartTransaction();
                Feature feature;

                do
                {
                    feature = layer.GetNextFeature();

                    if (feature != null)
                    {
                        object id;

                        if ((bool)idIsString)
                        {
                            id = feature.GetFieldAsString(featureList.IdFieldName);
                            UpdateFeature(ref feature, featureList.Bodies.First(x => (string)x.Id == (string)id));
                        }
                        else
                        {
                            id = feature.GetFieldAsInteger64(featureList.IdFieldName); // todo POSTGIS needs that feature.GetFID();
                            UpdateFeature(ref feature, featureList.Bodies.First(x => (long)x.Id == (long)id));
                        }

                        layer.SetFeature(feature);
                    }

                } while (feature != null);

                layer.CommitTransaction();
                layer.Dispose();
            }

            dataSource.SyncToDisk();
            dataSource.Dispose();
        }

        public static void Delete(string connection, string layerName, FeatureList featureList)
        {
            GdalConfiguration.ConfigureOgr();

            var dataSource = Ogr.Open(connection, 1);
            var layerCount = dataSource.GetLayerCount();

            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                var layer = dataSource.GetLayerByIndex(layerIndex);

                //if (layer.GetName() != layerName)
                //{
                //    continue;
                //}

                var featureCount = layer.GetFeatureCount(1);

                var idIsString = IsString(layer, featureList.IdFieldName);

                if (idIsString == null)
                {
                    throw new Exception("FeatureService (delete) id column has not supported field type (string or number)");
                }

                if ((bool)idIsString)
                {
                    layer.SetAttributeFilter($"{featureList.IdFieldName} IN ({string.Join(',', featureList.Bodies.Select(x => $"'{x.Id}'"))})");
                }
                else
                {
                    layer.SetAttributeFilter($"{featureList.IdFieldName} IN ({string.Join(',', featureList.Bodies.Select(x => x.Id.ToString()))})");
                }

                var featureCountFiltered = layer.GetFeatureCount(1);

                layer.StartTransaction();

                for (int i = 0; i < featureCountFiltered; i++)
                {
                    layer.ResetReading();
                    var feature = layer.GetNextFeature();
                    layer.DeleteFeature(feature.GetFID());
                }

                layer.CommitTransaction();
                layer.Dispose();
            }

            dataSource.Dispose();
        }

        private static void UpdateFeature(ref Feature feature, FeatureBody featureBody)
        {
            var featureDef = feature.GetDefnRef();
            var fieldCount = featureDef.GetFieldCount();

            for (int i = 0; i < fieldCount; i++)
            {
                var field = featureDef.GetFieldDefn(i);

                foreach (var property in featureBody.Properties)
                {
                    if (property.Key != field.GetName())
                    {
                        continue;
                    }

                    if (property.Value == null)
                    {
                        feature.SetField(property.Key, null);
                        continue;
                    }

                    var fieldType = Type.GetTypeCode(property.Value.GetType());

                    switch (fieldType)
                    {
                        case TypeCode.String:
                            feature.SetField(property.Key, (string)property.Value);
                            break;
                        case TypeCode.Int16:
                            feature.SetField(property.Key, (Int16)property.Value);
                            break;
                        case TypeCode.Int32:
                            feature.SetField(property.Key, (int)property.Value);
                            break;
                        case TypeCode.Int64:
                            feature.SetField(property.Key, (long)property.Value);
                            break;
                        case TypeCode.Boolean:
                            feature.SetField(property.Key, (int)property.Value);
                            break;
                        case TypeCode.Byte:
                            feature.SetField(property.Key, (byte)property.Value);
                            break;
                        case TypeCode.DateTime:
                            var dateTime = (DateTime)property.Value;
                            var tzFlag = dateTime.Kind == DateTimeKind.Utc ? 100 : (dateTime.Kind == DateTimeKind.Local ? 1 : 0);
                            feature.SetField(property.Key, dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, tzFlag );
                            break;
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            feature.SetField(property.Key, (double)property.Value);
                            break;
                        case TypeCode.Single:
                            feature.SetField(property.Key, (Single)property.Value);
                            break;
                        default:
                            // todo log ignored fields
                            break;
                    }
                }
            }

            if (featureBody.Id != null)
            {
                //todo feature.SetFID((long)featureBody.Id);
            }

            if (string.IsNullOrEmpty(featureBody.Geometry))
            {
                return;
            }

            var resultOgrGeometry = GeometryService.GetOgrGeometryFromWkt(featureBody.Geometry, 12);

            if (!resultOgrGeometry.Succesfully)
            {
                // todo log
                return;
            }

            // todo check spatial reference
            feature.SetGeometry(resultOgrGeometry.Value);
        }

        private static bool? IsString(OSGeo.OGR.Layer layer, string fieldName)
        {
            var layerDef = layer.GetLayerDefn();
            var fieldCount = layerDef.GetFieldCount();

            for (int i = 0; i < fieldCount; i++)
            {
                var field = layerDef.GetFieldDefn(i);

                if (field.GetName() != fieldName)
                {
                    continue;
                }

                var fieldType = field.GetFieldType();

                switch (fieldType)
                {
                    case FieldType.OFTString:
                    case FieldType.OFTWideString:
                        return true;
                    case FieldType.OFTInteger:
                    case FieldType.OFTInteger64:
                    case FieldType.OFTReal:
                        return false;
                    default:
                        return null;
                }
            }

            return null;
        }
    }
}
