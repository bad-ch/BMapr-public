namespace BMapr.GDAL.WebApi.Models
{
    public class Config
    {
        public string? Host { get; set; }
        public DirectoryInfo? AssemblyPath { get; set; }
        public DirectoryInfo? ApplicationRoot { get; set; }
        public DirectoryInfo? Data { get; set; }
        public DirectoryInfo? DataProjects { get; set; }
        public DirectoryInfo? DataShare { get; set; }
        public DirectoryInfo? Temp { get; set; }
        public Dictionary<string, string>? Placeholders { get; set; } = new();

        public bool? Cache { get; set; }

        public DirectoryInfo DataProject(string project)
        {
            return new DirectoryInfo(Path.Combine(DataProjects.FullName, project));
        }
    }
}
