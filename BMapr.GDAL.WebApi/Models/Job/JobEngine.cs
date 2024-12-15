using BMapr.GDAL.WebApi.Services;
using BMapr.GDAL.WebApi.Services.Jobs;
using Newtonsoft.Json;
using Quartz;
using System.Diagnostics;
using static Quartz.Logging.OperationName;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobEngine : IJob
    {
        private readonly ILogger<JobEngine> Logger;
        private readonly ISchedulerFactory Factory;
        private readonly IConfiguration IConfig;
        public readonly Config Config;

        public JobEngine(ILogger<JobEngine> logger, ISchedulerFactory factory, IConfiguration iConfig, IWebHostEnvironment environment)
        {
            Logger = logger;
            Factory = factory;
            IConfig = iConfig;
            Config = ConfigService.Get(iConfig, environment);
        }

        public Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;
            var processId = dataMap.GetIntValue("processId");

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
                return Task.FromResult(true);
            }
            else
            {
                Logger.LogInformation($"***********************************************************************");
                Logger.LogInformation($"**** Job-Engine is ENABLED");
                Logger.LogInformation($"***********************************************************************");
            }

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

                    AddJob(project, job, jobContent);
                }
            }

            return Task.FromResult(true);
        }

        public async void AddJob(string project, Job job, string jobContent)
        {
            var scheduler = await Factory.GetScheduler();
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
