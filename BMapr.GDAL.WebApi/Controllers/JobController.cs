using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Quartz;
using Quartz.Util;

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
            var allTriggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
            var jobsInfo = new List<object>();

            foreach (var triggerKey in allTriggerKeys)
            {
                var triggerDetails = await scheduler.GetTrigger(triggerKey);

                if (triggerDetails?.JobKey == null)
                {
                    continue;
                }

                var jobDetails = await scheduler.GetJobDetail(triggerDetails.JobKey);

                jobsInfo.Add(new
                {
                    TriggerKey = triggerDetails.Key.Name, 
                    JobKey = triggerDetails.JobKey.Name,
                    triggerDetails.StartTimeUtc,
                    triggerDetails.EndTimeUtc,
                    triggerDetails.FinalFireTimeUtc
                });
            }

            return Ok(jobsInfo);
        }
    }
}
