namespace BMapr.GDAL.WebApi.Models
{
    public class ProjectItem
    {
        public string Id { get; set; }

        public  string Name { get; set; }

        public bool ParseMapfile { get; set; }

        public double[] Extent { get; set; }
    }
}
