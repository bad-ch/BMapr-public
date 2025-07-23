namespace BMapr.GDAL.WebApi.Models.Db
{
    public class DbField
    {
        public bool? IsAutoIncrement { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public Type Type { get; set; }
        public Int32 ProviderType { get; set; }
        public int? MaxLength { get; set; }
        public bool IsNullable { get; set; } 
        public int Index { get; set; }
    }
}
