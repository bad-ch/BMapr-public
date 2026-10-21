using BMapr.GDAL.WebApi.Services;
using BMapr.GDAL.WebApi.Services.Jobs;
using Newtonsoft.Json;
using Quartz;
using System.Diagnostics;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobEngine : IJob
    {
        private readonly ILogger<JobEngine> Logger;
        private readonly IConfiguration IConfig;
        public readonly Config Config;

        public JobEngine(ILogger<JobEngine> logger, IConfiguration iConfig, IWebHostEnvironment environment)
        {
            Logger = logger;
            IConfig = iConfig;
            Config = ConfigService.Get(iConfig, environment);
        }

        public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var dataMap = context.JobDetail.JobDataMap;
            var processId = Convert.ToInt32(dataMap["processId"]);

            Logger.LogInformation($"*** Ini Jobs for all projects, process id {processId}");

            var dataProjects = Config.DataProjects?.FullName;

            if (!Directory.Exists(dataProjects) || Config.DataProjects == null)
            {
                throw new Exception("Path of Config.DataProjects not found");
            }

            if (IConfig.GetSection("Settings").GetValue<bool>("DisableJobEngine"))
            {
                Logger.LogInformation($"***********************************************************************");
                Logger.LogInformation($"**** Job-Engine is DISABLED");
                Logger.LogInformation($"***********************************************************************");
                return ValueTask.CompletedTask;
            }
            else
            {
                Logger.LogInformation($"***********************************************************************");
                Logger.LogInformation($"**** Job-Engine is ENABLED");
                Logger.LogInformation($"***********************************************************************");
            }

            var scheduler = context.Scheduler;

            foreach (var folder in Config.DataProjects?.GetDirectories()!)
            {
                var project = folder.Name;

                foreach (var jobFile in folder.GetFiles("*.job.json"))
                {
                    var jobContent = File.ReadAllText(jobFile.FullName);
                    var job = JsonConvert.DeserializeObject<Job>(jobContent, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    });

                    if (job == null)
                    {
                        Logger.LogError($"DES job fail {jobContent}, file {jobFile.FullName}");
                        continue;
                    }

                    AddJob(scheduler, project, job, jobContent);
                }
            }

            return ValueTask.CompletedTask;
        }

        public async void AddJob(IScheduler scheduler, string project, Job job, string jobContent)
        {
            var currentProcess = Process.GetCurrentProcess();

            IJobDetail jobDetail = JobBuilder.Create<JobGod>()
                .WithIdentity(job.Key)
                .UsingJobData("jobContent", jobContent)
                .UsingJobData("processId", currentProcess.Id)
                .UsingJobData("project", project)
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity($"{project}-{job.Key}-Trigger")
                .StartNow()
                .WithCronSchedule(job.CronExpression)
                .Build();

            await scheduler.ScheduleJob(jobDetail, trigger);
        }
    }
}
