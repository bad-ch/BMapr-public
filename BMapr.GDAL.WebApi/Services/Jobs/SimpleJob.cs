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

        public Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;

            var processId = dataMap.GetIntValue("processId");

            Logger.LogError($"*** TEST, process id {processId}");
            return Task.FromResult(true);
        }
    }
}
