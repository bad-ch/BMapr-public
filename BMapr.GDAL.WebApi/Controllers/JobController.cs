using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Quartz;
using Quartz.Impl.Matchers;

namespace BMapr.GDAL.WebApi.Controllers
{
    [ApiController]
    [Route("api/Job")]
    public class JobController : DefaultController
    {
        private readonly ISchedulerFactory _factory;
        private readonly ILogger<ProjectController> _logger;
        private readonly IMemoryCache _cache;

        public JobController(ISchedulerFactory factory, ILogger<ProjectController> logger, IConfiguration iConfig, IWebHostEnvironment environment, IMemoryCache cache) : base(iConfig, environment)
        {
            _logger = logger;
            _cache = cache;
            _factory = factory;
        }

        [HttpGet("List")]
        public async Task<IActionResult> List()
        {
            var scheduler = await _factory.GetScheduler();
            var allTriggerKeys = scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
            var jobsInfo = new List<object>();

            foreach (var triggerKey in allTriggerKeys.Result)
            {
                var triggerDetails = scheduler.GetTrigger(triggerKey);

                if (triggerDetails?.Result?.JobKey == null)
                {
                    continue;
                }

                var jobDetails = scheduler.GetJobDetail(triggerDetails.Result?.JobKey);

                jobsInfo.Add(new
                {
                    TriggerKey = triggerDetails.Result.Key.Name, 
                    JobKey = triggerDetails.Result.JobKey.Name,
                    triggerDetails.IsCompleted,
                    triggerDetails.IsCompletedSuccessfully,
                    triggerDetails.Status,
                    triggerDetails.Result.StartTimeUtc,
                    triggerDetails.Result.EndTimeUtc,
                    triggerDetails.Result.FinalFireTimeUtc,
                    nextFire = triggerDetails.Result.GetNextFireTimeUtc(),
                    lastFire = triggerDetails.Result.GetPreviousFireTimeUtc()
                });
            }

            return Ok(jobsInfo);
        }
    }
}
