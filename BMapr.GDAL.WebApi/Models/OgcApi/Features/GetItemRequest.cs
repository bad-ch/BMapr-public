namespace BMapr.GDAL.WebApi.Models.OgcApi.Features
{
    public class GetItemRequest
    {
        public string CollectionId { get; set; }

        public List<double> Bbox { get; set; }

        public string? BboxCrs { get; set; }

        public string? Crs { get; set; }

        public string Query { get; set; }

        public string Filter { get; set; }

        public int? Offset { get; set; }

        public int? Limit { get; set; }

        public string Format { get; set; }

        public string Url { get; set; }

        public string Host { get; set; }
    }
}
