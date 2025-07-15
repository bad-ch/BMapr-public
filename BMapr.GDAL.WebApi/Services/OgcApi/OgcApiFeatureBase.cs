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
        public static readonly string CrsWgs84 = "http://www.opengis.net/def/crs/OGC/1.3/CRS84";

        protected string UrlCollections { get; set; }
        protected List<CrsDefinition> CrsDefinitions { get; set; }
        protected Config Config { get; set; }
        protected string Project { get; set; }

        protected OgcApiFeatureBase(Config config,string project, string urlCollections, List<CrsDefinition> crsDefinitions)
        {
            Config = config;
            Project = project;
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


        public Result<Collection> GetCollectionItem(CollectionItem item)
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

        public int setCrs(FeatureCollection featureCollection, Result<FileContentResult> result, string crs, string bboxCrs)
        {
            int crsOut;

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
                    featureCollection.Crs = CrsWgs84;
                    crsOut = 4326;
                }
            }
            else
            {
                result.AddMessage("Set CRS from request");
                featureCollection.Crs = crs;
                crsOut = ProjectionService.getEPSGCode(crs);
            }

            return crsOut;
        }

        public int setBboxCrs(Result<FileContentResult> result, int crsOut, string bboxCrs)
        {
            int crsBboxOut;

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

            return crsBboxOut;
        }

        // *********************************************************************************************************************************
        // Generate Links
        // *********************************************************************************************************************************

        public Link GetHomeLink()
        {
            var urlCollections = $"{Config.Host}/api/ogcapi/features/{Project}/?f=json";

            return new Link
            {
                Href = urlCollections,
                Rel = "home",
                Title = "Landing page",
                Type = JsonMimeType,
            };
        }

        public Link GetCollectionsLink()
        {
            var urlCollections = $"{Config.Host}/api/ogcapi/features/{Project}/collections?f=json";

            return new Link
            {
                Href = urlCollections,
                Rel = "data",
                Title = "Get table of content",
                Type = JsonMimeType,
            };
        }

        public Link GetCollectionLink(string collectionId)
        {
            var urlCollections = $"{Config.Host}/api/ogcapi/features/{Project}/collections/{collectionId}?f=json";

            return new Link
            {
                Href = urlCollections,
                Rel = "collection",
                Title = "Get meta data of this data",
                Type = JsonMimeType,
            };
        }

        public Link GetQueryablesLink(string collectionId)
        {
            var urlCollections = $"{Config.Host}/api/ogcapi/features/{Project}/collections/{collectionId}/queryables?f=json";

            return new Link
            {
                Href = urlCollections,
                Rel = "queryables",
                Title = "Get schema data",
                Type = SchemaJsonMimeType,
            };
        }

        protected Link? GetNavigationLink(bool next, GetItemRequest request, int maxCount)
        {
            var urlCollections = $"{Config.Host}/api/ogcapi/features/{Project}/collections/{request.CollectionId}/items?";

            var flag = false;

            if (!string.IsNullOrEmpty(request.Format))
            {
                urlCollections += $"f={request.Format}";
                flag = true;
            }

            if (request.Bbox.Count > 0)
            {
                urlCollections += $"{(flag ? "&" : "")}bbox={string.Join(',', request.Bbox)}";
                flag = true;
            }

            if (!string.IsNullOrEmpty(request.BboxCrs))
            {
                urlCollections += $"{(flag ? "&" : "")}bbox-crs={request.BboxCrs}";
                flag = true;
            }

            if (!string.IsNullOrEmpty(request.Query))
            {
                urlCollections += $"{(flag ? "&" : "")}query={request.Query}";
                flag = true;
            }

            var offsetNew = 0;

            if (next)
            {
                offsetNew = (int)request.Offset! + (int)request.Limit!;
                if (offsetNew > maxCount)
                {
                    return null;
                }
            }
            else
            {
                offsetNew = (int)request.Offset! - (int)request.Limit!;
                if (offsetNew < 0)
                {
                    return null;
                }
            }

            urlCollections += $"{(flag ? "&" : "")}offset={offsetNew}";
            urlCollections += $"&limit={request.Limit}";

            return new Link
            {
                Href = urlCollections,
                Rel = next ? "next" : "prev",
                Title = $"{(next ? "Next" : "Previous")} page",
                Type = JsonMimeType,
            };
        }

        // *********************************************************************************************************************************
        // Helpers
        // *********************************************************************************************************************************

        private List<string> GetCrsList(List<string> crsList)
        {
            var crsListFormated = crsList.Select(x => x.Replace("epsg:", "")).Select(x => $"{EpsgCrsPrefix}{x}").ToList();
            return crsListFormated.Distinct().ToList();
        }

        // *********************************************************************************************************************************
        // Abstract definition
        // *********************************************************************************************************************************

        public abstract Result<FileContentResult> GetItems(GetItemRequest request);

        public abstract Result<Schema> GetQueryables(GetQueryablesRequest request);
    }
}
