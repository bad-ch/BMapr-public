namespace BMapr.GDAL.WebApi.Models
{
    public class ProjectSettings
    {
        public Guid Token { get; set; }
        
        public List<User> Users { get; set; } = new List<User>();

        public string Message { get; set; } = string.Empty;

        public ProjectSettings()
        {
            Token = Guid.NewGuid();
            Users = new ();
        }
    }
}
