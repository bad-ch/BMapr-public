using Quartz;

namespace BMapr.GDAL.WebApi.Services.Jobs
{
    public class SimpleJob: IJob
    {
        private readonly ILogger<SimpleJob> Logger;
        private readonly ISchedulerFactory Factory;

        public SimpleJob(ILogger<SimpleJob> logger, ISchedulerFactory factory)
        {
            Logger = logger;
            Factory = factory;
        }

        public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var dataMap = context.JobDetail.JobDataMap;

            var processId = Convert.ToInt32(dataMap["processId"]);

            Logger.LogError($"*** TEST, process id {processId}");
            return ValueTask.CompletedTask;
        }
    }
}
