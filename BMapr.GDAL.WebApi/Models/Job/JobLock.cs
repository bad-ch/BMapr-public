namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobLock
    {
        public int ProcessId { get; set; }
        public string JobKey { get; set; }
        public DateTime Start { get; set; }
        public long TimeOut { get; set; }
    }
}
