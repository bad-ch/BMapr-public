using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

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
                var trigger = await scheduler.GetTrigger(triggerKey);

                if (trigger?.JobKey == null)
                {
                    continue;
                }

                jobsInfo.Add(new
                {
                    TriggerKey = trigger.Key.Name,
                    JobKey = trigger.JobKey.Name,
                    trigger.StartTimeUtc,
                    trigger.EndTimeUtc,
                    trigger.FinalFireTimeUtc
                });
            }

            return Ok(jobsInfo);
        }
    }
}
