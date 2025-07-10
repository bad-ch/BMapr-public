using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.OgcApi.Features;
using BMapr.GDAL.WebApi.Models.Spatial;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.SqlClient;

namespace BMapr.GDAL.WebApi.Services.OgcApi
{
    public class OgcApiFeatureMsSql : OgcApiFeatureBase
    {
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

            crsOut = setCrs(featureCollection, result, request.Crs, request.BboxCrs);
            crsBboxOut = setBboxCrs(result, crsOut, request.BboxCrs);

            featureCollection.Links.Add(GetHomeLink());
            featureCollection.Links.Add(GetCollectionsLink());
            featureCollection.Links.Add(GetCollectionLink(request.CollectionId));
            featureCollection.Links.Add(GetQueryablesLink(request.CollectionId));

            using (var connection = new SqlConnection(request.ConnectionString))
            {
                try
                {
                    connection.Open();

                    using (var command = new SqlCommand(request.Data, connection))
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
    }
}
