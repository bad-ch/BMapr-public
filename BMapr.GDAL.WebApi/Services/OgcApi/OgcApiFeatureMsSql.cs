using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.OgcApi.Features;
using BMapr.GDAL.WebApi.Models.Spatial;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Text;

namespace BMapr.GDAL.WebApi.Services.OgcApi
{
    public class OgcApiFeatureMsSql : OgcApiFeatureBase
    {
        public static readonly string Connectionstring = "Connectionstring";
        public static readonly string DataTable = "DataTable";
        public static readonly string DataWhere = "DataWhere";                         // where clause, base filter, will be extended with bbox and query
        public static readonly string DataGeometryField = "DataGeometryField";
        public static readonly string DataGeometrySrid = "DataGeometrySrid";          // Srid of the geometry field in the database, could be 0 as well
        public static readonly string DataGeometryEpsgCode = "DataGeometryEpsgCode";  // EPSG code of the geometry field

        public readonly List<string> Parameters = new() { Connectionstring, DataTable, DataWhere, DataGeometryField, DataGeometrySrid, DataGeometryEpsgCode };

        private CacheService cacheService { get; set; }

        public OgcApiFeatureMsSql(Config config, string project, string urlCollections, List<CrsDefinition> crsDefinitions) : base(config, project, urlCollections, crsDefinitions)
        {
        }

        public override Result<FileContentResult> GetItems(GetItemRequest request)
        {
            cacheService = new CacheService(Config.DataProject(Project).FullName, request.Host, "oaf", request.CollectionId);

            var cachedContent = cacheService.GetContent(request.Url);

            if (cachedContent != null)
            {
                return new Result<FileContentResult> {Value = cachedContent};
            }

            var featureCollection = new FeatureCollection { Type = "FeatureCollection" };
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
                            featureMatched = Convert.ToInt32(result);
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

                    var sql = $"SELECT * FROM {(string)request.ConnectionParameters[DataTable]} WHERE {GetWhere(request, crsBboxOut)}";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    Console.WriteLine(reader[0]);
                                }
                            }
                            else
                            {
                                Console.WriteLine("No rows found.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }

            // todo
            return null;
        }

        public override Result<Schema> GetQueryables(GetQueryablesRequest request)
        {
            throw new NotImplementedException();
        }

        #region private

        private Result<FileContentResult> GetFinalResult(Result<FileContentResult> result, GetItemRequest request, FeatureCollection featureCollection)
        {
            var content = JsonConvert.SerializeObject(featureCollection);
            var byteContent = Encoding.UTF8.GetBytes(content);
            var contentType = "application/geo+json";

            cacheService.WriteContent(request.Url, byteContent, contentType);

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
                $"{name}.STIntersects(geometry::STGeomFromText('${wktBbox}', {srid})) = 1";
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

        #endregion
    }
}
