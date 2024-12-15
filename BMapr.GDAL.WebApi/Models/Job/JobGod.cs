using BMapr.GDAL.WebApi.Services;
using Newtonsoft.Json;
using Quartz;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobGod : IJob
    {
        private readonly ILogger<JobGod> Logger;
        private readonly Config Config;
        private string JobPath;

        public JobGod(ILogger<JobGod> logger, IConfiguration iConfig, IWebHostEnvironment environment)
        {
            Logger = logger;
            Config = ConfigService.Get(iConfig, environment);
            JobPath = Path.Combine(Config.Data.FullName, "jobs");
        }

        public Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;
            var jobContent = dataMap.GetString("jobContent");
            var processId = dataMap.GetIntValue("processId");
            var project = dataMap.GetString("project");

            if (string.IsNullOrEmpty(jobContent))
            {
                throw new Exception("Job content is not valid");
            }

            var job = JsonConvert.DeserializeObject<Job>(jobContent, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            var jobDbFile = Path.Combine(JobPath, "jobs.db");

            var jobDbService = new JobDbService(jobDbFile);
            var dt = RoundUp(DateTime.Now, TimeSpan.FromMinutes(1));
            var reference = $"{project}_{job.Key}_{dt:yyyMMddHHmm}";

            var resultJob = jobDbService.AddJob(reference, job.Key, jobContent);

            if (!resultJob.Succesfully)
            {
                if (resultJob.Exceptions.Any())
                {
                    Logger.LogError(string.Join(",", resultJob.Exceptions.Select(x => x.Message)));
                }
                Logger.LogInformation($"Job canceled because already started, process {processId}");
                return Task.FromResult(true);
            }

            var counter = 0;
            var resultItems = new Dictionary<string, JobActionResultItem>();

            foreach (var action in job.Actions)
            {
                action.Logger = Logger;
                action.Project = project;
                action.Config = Config;
                action.ProcessId = processId;
                action.ResultItems = resultItems;

                var result = action.Execute();

                if (!result.Succesfully)
                {
                    Logger.LogError(string.Join(",", result.Messages));
                }

                if(result.Succesfully && result.Messages.Any())
                {
                    Logger.LogInformation(string.Join(",", result.Messages));
                }

                resultItems = action.ResultItems;
                counter++;
            }

            Logger.LogInformation($"GodJob, process id {processId}");

            return Task.FromResult(true);
        }

        private DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }
    }
}
