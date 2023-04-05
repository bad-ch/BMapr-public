namespace BMapr.GDAL.WebApi.Models.Job
{
    public enum JobActionResultItemType
    {
        TextFile,
        BinaryFile,
        HtmlContent,
        File,
        Hash,
        ResultJint,
        String,
        Json
    }

    public class JobActionResultItem
    {
        public JobActionResultItemType Type { get; set; }
        public object Value { get; set; }
    }
}
