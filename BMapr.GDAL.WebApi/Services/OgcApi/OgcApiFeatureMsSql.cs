using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.Db;
using BMapr.GDAL.WebApi.Models.OgcApi.Features;
using BMapr.GDAL.WebApi.Models.Spatial;
using BMapr.GDAL.WebApi.Models.Spatial.Vector;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Types;
using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace BMapr.GDAL.WebApi.Services.OgcApi
{
    public class OgcApiFeatureMsSql : OgcApiFeatureBase
    {
        public static readonly string Connectionstring = "Connectionstring";
        public static readonly string DataTable = "DataTable";
        public static readonly string DataWhere = "DataWhere";                        // where clause, base filter, will be extended with bbox and query
        public static readonly string DataIdField = "DataIdField";
        public static readonly string DataGeometryField = "DataGeometryField";
        public static readonly string DataUseGeography = "DataUseGeography";          // boolean use geography data type
        public static readonly string DataGeometrySrid = "DataGeometrySrid";          // Srid of the geometry field in the database, could be 0 as well
        public static readonly string DataGeometryEpsgCode = "DataGeometryEpsgCode";  // EPSG code of the geometry field

        public readonly List<string> Parameters = new() { Connectionstring, DataTable, DataWhere, DataIdField, DataGeometryField, DataUseGeography, DataGeometrySrid, DataGeometryEpsgCode };

        private CacheService cacheService { get; set; }

        public OgcApiFeatureMsSql(Config config, string project, List<CrsDefinition> crsDefinitions) : base(config, project, crsDefinitions)
        {
        }

        public override Result<FileContentResult> GetItems(GetItemRequest request)
        {
            cacheService = new CacheService(Config.DataProject(Project).FullName, request.Host, "oaf", request.CollectionId);

            // todo reactivate

            //var cachedContent = cacheService.GetContent(request.Url);

            //if (cachedContent != null)
            //{
            //    return new Result<FileContentResult> {Value = cachedContent};
            //}

            var featureCollection = new Models.OgcApi.Features.FeatureCollection { Type = "FeatureCollection" };
            var result = new Result<FileContentResult>();
            var crsOut = 0;
            var crsBboxOut = 0;

            var checkParametersResult = CheckParameters(request);

            if (!checkParametersResult.Value)
            {
                result.TakeoverEvents(checkParametersResult);
                result.Value = null;
                return result;
            }

            featureCollection.Name = request.CollectionId;

            crsOut = setCrs(featureCollection, result, request.Crs, request.BboxCrs);
            crsBboxOut = setBboxCrs(result, crsOut, request.BboxCrs);

            featureCollection.Links.Add(GetHomeLink());
            featureCollection.Links.Add(GetCollectionsLink());
            featureCollection.Links.Add(GetCollectionLink(request.CollectionId));
            featureCollection.Links.Add(GetQueryablesLink(request.CollectionId));

            using (var connection = new SqlConnection((string)request.ConnectionParameters[Connectionstring]))
            {
                try
                {
                    connection.Open();

                    var sqlCount = $"SELECT Count(*) FROM {(string)request.ConnectionParameters[DataTable]} WHERE {GetWhere(request, crsBboxOut)}";
                    int? featureMatched = null; 

                    using (var command = new SqlCommand(sqlCount, connection))
                    {
                        var counter = command.ExecuteScalar();

                        if (counter != null)
                        {
                            featureMatched = Convert.ToInt32(counter);
                        }
                    }

                    if (featureMatched == null)
                    {
                        result.AddMessage("Failed to count features in the database.", null, Result.LogLevel.Error);
                        result.Value = null;
                        return result;
                    }

                    if (featureMatched == 0)
                    {
                        result.AddMessage("No features found in the database for the given parameters.", null, Result.LogLevel.Info);
                        return GetFinalResult(result, request, featureCollection);
                    }

                    featureCollection.NumberMatched = (int)featureMatched;
                    result.Messages.Add($"feature count with filter {featureCollection.NumberMatched}");

                    if (request.Offset == null && request.Limit != null && request.Limit < featureCollection.NumberMatched)
                    {
                        result.Messages.Add($"force offset - because not set - to 0");
                        request.Offset = 0;
                    }

                    if (request.Offset != null && request.Limit != null)
                    {
                        var next = GetNavigationLink(true, request, (int)featureCollection.NumberMatched);
                        var prev = GetNavigationLink(false, request, (int)featureCollection.NumberMatched);

                        if (next != null)
                        {
                            featureCollection.Links.Add(next);
                        }

                        if (prev != null)
                        {
                            featureCollection.Links.Add(prev);
                        }
                    }

                    var fields = GetFields(connection, request);

                    var sql = $"SELECT * FROM {(string)request.ConnectionParameters[DataTable]} WHERE {GetWhere(request, crsBboxOut)} ORDER BY {(string)request.ConnectionParameters[DataIdField]} OFFSET {request.Offset} ROWS FETCH NEXT {request.Limit} ROWS ONLY;";

                    result.Messages.Add($"sql queried: {sql}");

                    using (var command = new SqlCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            int j = 0;

                            while (reader.Read())
                            {
                                var feature = new Models.OgcApi.Features.Feature { Type = "Feature" };

                                fields.ForEach(field =>
                                {
                                    if (field.Type == null)
                                    {
                                        return;
                                    }

                                    var fieldType = field.Type.ToString();

                                    switch (fieldType)
                                    {
                                        case "System.String":
                                        case "System.Char":
                                            feature.Properties.Add(field.Name, reader.GetString(field.Name));
                                            break;
                                        case "System.Guid":
                                            feature.Properties.Add(field.Name, reader.GetGuid(field.Name));
                                            break;
                                        case "System.Boolean":
                                            feature.Properties.Add(field.Name, reader.GetBoolean(field.Name));
                                            break;
                                        case "System.Byte":
                                        case "System.SByte":
                                            feature.Properties.Add(field.Name, reader.GetByte(field.Name));
                                            break;
                                        case "System.Decimal":
                                            feature.Properties.Add(field.Name, reader.GetDecimal(field.Name));
                                            break;
                                        case "System.Double":
                                        case "System.Single":
                                            feature.Properties.Add(field.Name, reader.GetDouble(field.Name));
                                            break;
                                        case "System.Int32":
                                        case "System.UInt32":
                                            feature.Properties.Add(field.Name, reader.GetInt32(field.Name));
                                            break;
                                        //case "System.IntPtr":
                                        //case "System.UIntPtr":
                                        case "System.Int64":
                                        case "System.UInt64":
                                        //case "System.Int16":
                                        //case "System.UInt16":
                                            feature.Properties.Add(field.Name, reader.GetInt64(field.Name));
                                            break;
                                        case "System.DateTime":
                                            feature.Properties.Add(field.Name, reader.GetDateTime(field.Name));
                                            break;
                                        //case "System.DateTimeOffset":
                                        //    feature.Properties.Add(field.Name, reader.GetDateTimeOffset(field.Name));
                                        //    break;
                                        default:
                                            break;
                                            throw new Exception($"Type not found {field.Name}");
                                    }
                                });

                                var geometryFieldName = (string) request.ConnectionParameters[DataGeometryField];
                                var fieldGeometry = fields.FirstOrDefault(x => x.Name == geometryFieldName);

                                // todo handle string,guid ids as well
                                var idFieldName = (string)request.ConnectionParameters[DataIdField];
                                feature.Id = reader.GetInt32(idFieldName).ToString();

                                if (fieldGeometry != null)
                                {
                                    var sqlBytes = reader.GetSqlBytes(fields.First(x => x.Name == geometryFieldName).Index);

                                    if (sqlBytes != null)
                                    {
                                        //string hexWKB = BitConverter.ToString(sqlBytes.Value).Replace("-", "");
                                        //SqlGeometry geometrySql = SqlGeometry.STGeomFromWKB(sqlBytes, (int)request.ConnectionParameters[DataGeometrySrid]);
                                        //var wkt = geometrySql.STAsText().Value.ToString();

                                        //byte[] geometryWKB = reader[geometryFieldName] as byte[];
                                        //var geometryWKB = reader.GetBytes(geometryFieldName,);
                                        //var wkbReader = new NetTopologySuite.IO.WKBReader();
                                        //var geometryNT = wkbReader.Read(sqlBytes.Value);
                                        //string wkt = geometryNT.ToText(); // Convert to WKT

                                        object geometryData = reader[fields.First(x => x.Name == geometryFieldName).Index].ToString();
                                        var wkt = geometryData.ToString();

                                        //string wkt = reader.GetString(fields.First(x => x.Name == geometryFieldName).Index);

                                        var geometry = GeometryService.GetOgrGeometryFromWkt(wkt);
                                        var geometryGeoJson = GeometryService.GetStringFromOgrGeometry(geometry.Value, "geojson", false);

                                        if (!string.IsNullOrEmpty(geometryGeoJson))
                                        {
                                            feature.Geometry = JsonConvert.DeserializeObject<dynamic>(geometryGeoJson)!;
                                        }
                                    }

                                }

                                featureCollection.Features.Add(feature);
                                j++;
                            }

                            featureCollection.NumberReturned = j;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }

            var content = JsonConvert.SerializeObject(featureCollection);
            var byteContent = Encoding.UTF8.GetBytes(content);
            var contentType = "application/geo+json";

            // todo reactivate

            // cacheService.WriteContent(request.Url, byteContent, contentType);

            result.Value = new FileContentResult(byteContent, contentType);
            result.Succesfully = true;
            return result;
        }

        public override Result<Schema> GetQueryables(GetQueryablesRequest request)
        {
            throw new NotImplementedException();
        }

        #region private

        private Result<FileContentResult> GetFinalResult(Result<FileContentResult> result, GetItemRequest request, Models.OgcApi.Features.FeatureCollection featureCollection)
        {
            var content = JsonConvert.SerializeObject(featureCollection);
            var byteContent = Encoding.UTF8.GetBytes(content);
            var contentType = "application/geo+json";

            // todo reactivate

            // cacheService.WriteContent(request.Url, byteContent, contentType);

            result.Value = new FileContentResult(byteContent, contentType);
            result.Succesfully = true;
            return result;
        }

        private string GetWhere(GetItemRequest request, int crsBboxOut)
        {
            var dataWhere = request.ConnectionParameters[DataWhere] as string;
            var bboxWhere = GetBBoxSpatialFilter(request, crsBboxOut);
            var queryWhere = request.Query;

            var conditions = new List<string>();

            if (!string.IsNullOrEmpty(dataWhere))
            {
                conditions.Add(dataWhere);
            }

            if (!string.IsNullOrEmpty(bboxWhere))
            {
                conditions.Add(bboxWhere);
            }

            if (!string.IsNullOrEmpty(queryWhere))
            {
                conditions.Add(queryWhere);
            }

            return string.Join(" AND ", conditions);
        }

        private string GetBBoxSpatialFilter(GetItemRequest request, int crsBboxOut)
        {
            string name = (string)request.ConnectionParameters[DataGeometryField];
            int srid = (int)request.ConnectionParameters[DataGeometrySrid];
            string wktBbox = string.Empty;

            if (crsBboxOut == 4326)
            {
                wktBbox = $"POLYGON(({request.Bbox[1]} {request.Bbox[0]}, {request.Bbox[1]} {request.Bbox[2]},{request.Bbox[3]} {request.Bbox[2]}, {request.Bbox[3]} {request.Bbox[0]}, {request.Bbox[1]} {request.Bbox[0]}))";
            }
            else
            {
                wktBbox = $"POLYGON(({request.Bbox[0]} {request.Bbox[1]}, {request.Bbox[2]} {request.Bbox[1]},{request.Bbox[2]} {request.Bbox[3]}, {request.Bbox[0]} {request.Bbox[3]}, {request.Bbox[0]} {request.Bbox[1]}))";
            }

            // todo

            //if (crsBboxOut != crsSrc)
            //{
            //    var outBboxCrs = new OSGeo.OSR.SpatialReference("");
            //    outBboxCrs.ImportFromEPSG(crsBboxOut);
            //    var t1 = outCrs.GetAxisOrientation(null, 0);
            //    var t2 = outCrs.GetAxisOrientation(null, 1);


            //    var srcCrs = new OSGeo.OSR.SpatialReference("");
            //    srcCrs.ImportFromEPSG(crsSrc);
            //    var t3 = srcCrs.GetAxisOrientation(null, 0);
            //    var t4 = srcCrs.GetAxisOrientation(null, 1);

            //    var coordTransBbox = new OSGeo.OSR.CoordinateTransformation(outBboxCrs, srcCrs);

            //    geometryBbox.Transform(coordTransBbox);
            //}

            return
                $"{name}.STIntersects(geometry::STGeomFromText('{wktBbox}', {srid})) = 1";
        }

        private Result<bool> CheckParameters(GetItemRequest request)
        {
            var result = new Result<bool>
            {
                Value = true
            };

            Parameters.ForEach(parameter =>
            {
                if (!request.ConnectionParameters.ContainsKey(parameter))
                {
                    result.AddMessage($"Parameter '{parameter}' is missing.", null, Result.LogLevel.Error);
                    result.Value = false;
                }
            });

            // todo check specific content of parameters

            return result;
        }

        private List<DbField> GetFields(SqlConnection connection, GetItemRequest request)
        {
            var fields = new List<DbField>();

            using (SqlCommand command = new SqlCommand($"SELECT * FROM {(string)request.ConnectionParameters[DataTable]}", connection))
            {
                using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                {
                    DataTable? schemaTable = reader.GetSchemaTable();

                    var index = 0;

                    foreach (DataRow colRow in schemaTable?.Rows)
                    {
                        var field = new DbField()
                        {
                            IsAutoIncrement = colRow.Field<bool>("IsAutoIncrement"),
                            Name = colRow.Field<String>("ColumnName") ?? "",
                            Type = colRow.Field<System.Type>("DataType"),
                            TypeName = (colRow.Field<System.Type>("DataType"))?.ToString(),
                            ProviderType = colRow.Field<Int32>("ProviderType"),
                            MaxLength = colRow.Field<Int32>("ColumnSize"),
                            IsNullable = colRow.Field<bool>("AllowDBNull"),
                            Index = index
                        };

                        index++;
                        fields.Add(field);
                    }
                }
            }

            return fields;
        }

        private DbFieldValue GetFieldValue(DbField dbField, string value)
        {
            return new DbFieldValue()
            {
                IsAutoIncrement = dbField.IsAutoIncrement,
                Name = dbField.Name,
                Type = dbField.Type,
                ProviderType = dbField.ProviderType,
                TypeName = dbField.TypeName,
                MaxLength = dbField.MaxLength,
                IsNullable = dbField.IsNullable,
                Value = value
            };
        }

        /// <summary>
        /// Convert from feature to db fields list, needed for transaction
        /// </summary>
        /// <param name="request"></param>
        /// <param name="featureBody"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        
        private List<DbFieldValue> GetValues(GetItemRequest request, FeatureBody featureBody, List<DbField> fields)
        {
            var properties = new List<DbFieldValue>();

            foreach (var column in featureBody.Properties)
            {
                foreach (var field in fields)
                {
                    if (column.Key != field.Name)
                    {
                        continue;
                    }

                    var fieldType = field.Type.ToString();

                    switch (fieldType)
                    {
                        case "System.String":
                        case "System.Char":
                        case "System.Guid":
                            properties.Add(GetFieldValue(field, column.Value == null ? "NULL" : $"'{column.Value}'"));
                            break;
                        case "System.Boolean":
                        case "System.Byte":
                        case "System.SByte":
                        case "System.Decimal":
                        case "System.Double":
                        case "System.Single":
                        case "System.Int32":
                        case "System.UInt32":
                        case "System.IntPtr":
                        case "System.UIntPtr":
                        case "System.Int64":
                        case "System.UInt64":
                        case "System.Int16":
                        case "System.UInt16":
                            properties.Add(GetFieldValue(field, column.Value == null ? "NULL" : $"{column.Value}"));
                            break;
                        case "System.DateTime":
                            properties.Add(GetFieldValue(field, column.Value == null ? "NULL" : $"'{column.Value}'")); // todo check
                            break;
                        case "System.DateTimeOffset":
                            properties.Add(GetFieldValue(field, column.Value == null ? "NULL" : $"CAST('{column.Value}' AS DATETIMEOFFSET)"));
                            break;
                        default:
                            throw new Exception($"Type not found {field.Name}");
                    }
                }
            }

            var useGeographyType = (bool) request.ConnectionParameters[DataUseGeography];

            if (!string.IsNullOrEmpty(featureBody.Geometry) && fields.Any(x => x.ProviderType == 29) && !useGeographyType)
            {
                var field = fields.First(x => x.ProviderType == 29); //what is if we have more than one geometry field
                properties.Add(GetFieldValue(field, $"geometry::STGeomFromText('{featureBody.Geometry}', {featureBody.Epsg})"));
            }

            // for geography the provider type is 29 as well, that's quite ugly
            if (!string.IsNullOrEmpty(featureBody.Geometry) && fields.Any(x => x.ProviderType == 29) && useGeographyType)
            {
                var invertedGeometry = GeometryService.InvertWktGeometry(featureBody.Geometry);

                var field = fields.First(x => x.ProviderType == 29); //what is if we have more than one geometry field
                properties.Add(GetFieldValue(field, $"geography::STGeomFromText('{invertedGeometry.Value}', {featureBody.Epsg})"));
            }

            return properties;
        }

        #endregion
    }
}
