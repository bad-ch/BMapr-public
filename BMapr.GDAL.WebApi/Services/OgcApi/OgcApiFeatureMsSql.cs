using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.OgcApi.Features;
using BMapr.GDAL.WebApi.Models.Spatial;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

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

        public OgcApiFeatureMsSql(Config config, string project, string urlCollections, List<CrsDefinition> crsDefinitions) : base(config, project, urlCollections, crsDefinitions)
        {
        }

        public override Result<FileContentResult> GetItems(GetItemRequest request)
        {

            var cacheService = new CacheService(Config.DataProject(Project).FullName, request.Host, "oaf", request.CollectionId);

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

                    var sql = $"SELECT * FROM {(string)request.ConnectionParameters[DataTable]} WHERE ${this.GetBBoxSpatialFilter(request, crsBboxOut)}"; // todo add query and DataWhere

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
