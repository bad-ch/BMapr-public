namespace BMapr.GDAL.WebApi.Models.Job
{
    public class Job
    {
        public string Key { get; set; }
        public string CronExpression { get; set; }

        public List<JobAction> Actions { get; set; }

        public Job()
        {
            Actions = new List<JobAction>();
        }
    }
}
