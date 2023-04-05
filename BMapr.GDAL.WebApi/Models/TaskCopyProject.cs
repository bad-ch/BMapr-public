namespace BMapr.GDAL.WebApi.Models
{
    /// <summary>
    /// result of the copy task from the uploaded data to the project folder
    /// </summary>
    public class TaskCopyProject
    {
        public List<string> AllowedFiles { get; set; }
        public List<string> ForbiddenFiles { get; set; }

        public TaskCopyProject()
        {
            AllowedFiles = new List<string>();
            ForbiddenFiles = new List<string>();
        }
    }
}
