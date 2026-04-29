namespace BMapr.GDAL.WebApi.Models.Db
{
    public class DbFieldValue : DbField
    {
        public string Value { get; set; }
        public byte[]? RawValue { get; set; }
    }
}
