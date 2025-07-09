using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Models.OgcApi.Features;
using BMapr.GDAL.WebApi.Models.Spatial;
using Microsoft.AspNetCore.Mvc;

namespace BMapr.GDAL.WebApi.Services.OgcApi
{
    public abstract class OgcApiFeatureBase
    {
        public static readonly string JsonMimeType = "application/json";
        public static readonly string GeoJsonMimeType = "application/geo+json";
        public static readonly string SchemaJsonMimeType = "application/schema+json";
        public static readonly string EpsgCrsPrefix = "http://www.opengis.net/def/crs/EPSG/0/";

        private string UrlCollections { get; set; }
        private List<CrsDefinition> CrsDefinitions { get; set; }

        protected OgcApiFeatureBase(string urlCollections, List<CrsDefinition> crsDefinitions)
        {
            UrlCollections = urlCollections;
            CrsDefinitions = crsDefinitions;
        }

        public Result<Collections> GetCollections(List<CollectionItem> items)
        {
            var collections = new Collections();

            items.ForEach(item =>
            {
                collections.CollectionList.Add(GetCollectionItem(item).Value);
            });

            return new Result<Collections> { Value = collections, Succesfully = true };
        }


        public Result<Collection> GetCollection(CollectionItem item)
        {
            var collection = GetCollectionItem(item);
            return new Result<Collection> { Value = collection.Value, Succesfully = true };
        }


        private Result<Collection> GetCollectionItem(CollectionItem item)
        {
            var collection = new Collection
            {
                Id = item.Name,
                Title = item.Title,
                Description = item.Description,
                Extent = new BMapr.GDAL.WebApi.Models.OgcApi.Features.Extent
                {
                    Temporal = new Temporal(),
                    Spatial = new Spatial
                    {
                        Crs = item.SpatialCrs,
                    },
                },
            };

            collection.StorageCrs = item.StorageCrs;
            collection.Crs.Add(item.DefaultCrs);

            var listLayer = GetCrsList(item.AdditionalCrs);

            if (listLayer.Count > 0)
            {
                collection.Crs.AddRange(listLayer);
            }

            collection.Crs = collection.Crs.Distinct().ToList();

            collection.Extent.Spatial.Bbox.Add(new List<double> { item.MinX, item.MinY, item.MaxX, item.MaxY });

            collection.Links.Add(new Link { Rel = "self", Title = "This collection", Type = JsonMimeType, Href = $"{UrlCollections}/{item.Name}" });
            collection.Links.Add(new Link { Rel = "items", Title = $"{item.Name} as GeoJSON", Type = GeoJsonMimeType, Href = $"{UrlCollections}/{item.Name}/items?f=geojson" });
            collection.Links.Add(new Link { Rel = "queryables", Title = $"Get available attributes", Type = SchemaJsonMimeType, Href = $"{UrlCollections}/{item.Name}/queryables?f=json" });
            // collection.Links.Add(new Link() { Rel = "describedby", Title = $"Schema for {item.Name}", Type = JsonMimeType, Href = $"{UrlCollections}/{item.Name}/schema?f=application/json" });

            return new Result<Collection> { Value = collection, Succesfully = true };
        }

        private List<string> GetCrsList(List<string> crsList)
        {
            var crsListFormated = crsList.Select(x => x.Replace("epsg:", "")).Select(x => $"{EpsgCrsPrefix}{x}").ToList();
            return crsListFormated.Distinct().ToList();
        }

        public abstract Result<FileContentResult> GetItems(GetItemRequest request);

        public abstract Result<Schema> GetQueryables(GetQueryablesRequest request);
    }
}
