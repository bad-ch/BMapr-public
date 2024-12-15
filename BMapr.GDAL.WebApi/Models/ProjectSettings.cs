namespace BMapr.GDAL.WebApi.Models
{
    public class ProjectSettings
    {
        public Guid Token { get; set; }
        public string Message { get; set; } = string.Empty;

        public ProjectSettings()
        {
            Token = Guid.NewGuid();
        }
    }
}
