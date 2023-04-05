namespace BMapr.GDAL.WebApi.Models
{
    public class ProjectSettings
    {
        public Guid Token { get; set; }

        public ProjectSettings()
        {
            Token = Guid.NewGuid();
        }
    }
}
