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

        public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
        {
            var dataMap = context.JobDetail.JobDataMap;

            var processId = dataMap.GetInt("processId");

            Logger.LogError($"*** TEST, process id {processId}");
            return new ValueTask(Task.FromResult(true));
        }
    }
}
