using System.Text;
using BMapr.GDAL.WebApi.Models.Tracking;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using BMapr.GDAL.WebApi.Services;
using LiteDB;
using Newtonsoft.Json;

namespace BMapr.GDAL.WebApi.Controllers
{
    [Route("api/Tracking")]
    [ApiController]
    public class TrackingController : DefaultController
    {
        private readonly ILogger<TrackingController> _logger;
        private readonly IMemoryCache _cache;

        public TrackingController(ILogger<TrackingController> logger, IConfiguration iConfig, IWebHostEnvironment environment, IMemoryCache cache) : base(iConfig, environment)
        {
            _logger = logger;
            _cache = cache;
        }

        [BasicAuthorization]
        [HttpPost("PushLocation/{project}")]
        public IActionResult PushLocation(string project)
        {
            var bodyContent = Request.GetRawBodyBytesAsync().Result;
            var bodyContentType = Request.ContentType;

            if (bodyContent == null || bodyContent.Length == 0 || string.IsNullOrEmpty(bodyContentType))
            {
                return NoContent();
            }

            var dataPath = Config.Data?.FullName;
            var trackingPath = Path.Combine(dataPath, "tracking");
            var projectPath = Path.Combine(trackingPath, project);

            if (!Directory.Exists(projectPath))
            {
                return BadRequest("Project path don't exists");
            }

            var content = Encoding.UTF8.GetString(bodyContent);
            Location? location;

            try
            {
                location = JsonConvert.DeserializeObject<Location>(content);
            }
            catch (Exception e)
            {
                return BadRequest();
            }

            if (location == null)
            {
                return BadRequest();
            }

            //todo check for characters a-z,A-Z,0-9
            var trackFolder = $"{location.Tid}";
            var trackPath = Path.Combine(projectPath, trackFolder);

            if (!Directory.Exists(trackPath))
            {
                Directory.CreateDirectory(trackPath);
            }

            var ticks = DateTime.Now.Ticks;
            var guid = Guid.NewGuid();
            var fileName = $"{ticks}_{guid}.json";
            var filePath = Path.Combine(trackPath, fileName);

            try
            {
                System.IO.File.WriteAllBytes(filePath, bodyContent);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error write location tracking: {ex.Message}");
                return BadRequest(ex.Message);
            }

            return Ok(new List<object>());
        }

        /// <summary>
        /// Save location data from Owntracks in a file database 
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        [BasicAuthorization]
        [HttpPost("PushLocationV2/{project}")]
        public IActionResult PushLocationV2(string project, [FromBody] Location? location)
        {
            var dataPath = Config.Data?.FullName;
            var trackingPath = Path.Combine(dataPath, "tracking");
            var projectPath = Path.Combine(trackingPath, project);

            if (location == null)
            {
                return NoContent();
            }

            if (!Directory.Exists(projectPath))
            {
                return BadRequest("Project path don't exists");
            }

            //todo check for characters a-z,A-Z,0-9
            var dbName = $"{location.Tid}.db";
            var dbPath = Path.Combine(projectPath, dbName);

            using (var db = new LiteDatabase($"Filename={dbPath};connection=shared")) //get or new
            {
                var col = db.GetCollection<Location>("locations"); //get or new

                col.Insert(location);
                col.EnsureIndex(x => x.TimestampSecondsLocationFix);
            }

            var positionFriends = new List<object>();

            positionFriends.AddRange(TrackingService.GetCards(projectPath, new List<string>() { location.Tid }));
            positionFriends.AddRange(TrackingService.GetCurrentPosition(projectPath, new List<string>() { location.Tid }));

            return Ok(positionFriends);
        }

        /// <summary>
        /// Save card data from Owntracks in a file database 
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        [BasicAuthorization]
        [HttpPost("PushCard/{project}")]
        public IActionResult PushCard(string project, [FromBody] Card? card)
        {
            var dataPath = Config.Data?.FullName;
            var trackingPath = Path.Combine(dataPath, "tracking");
            var projectPath = Path.Combine(trackingPath, project);

            if (card == null)
            {
                return NoContent();
            }

            if (!Directory.Exists(projectPath))
            {
                return BadRequest($"Project {project} path don't exist");
            }

            var dbName = $"cards.db";
            var dbPath = Path.Combine(projectPath, dbName);

            using (var db = new LiteDatabase($"Filename={dbPath};connection=shared")) //get or new
            {
                var col = db.GetCollection<Card>("cards"); //get or new

                var cardExist = col.Query().Where(x => x.Tid == card.Tid).FirstOrDefault();

                if (cardExist == null)
                {
                    col.Insert(card);
                    col.EnsureIndex(x => x.Tid);
                    return Ok("card inserted");
                }

                cardExist.Name = card.Name;
                cardExist.Face = card.Face;
                col.Update(cardExist);

                return Ok("card updated");
            }
        }

        [HttpGet("GetLatestPositions/{project}")]
        public IActionResult GetLatestPositions(string project)
        {
            var dataPath = Config.Data?.FullName;
            var trackingPath = Path.Combine(dataPath, "tracking");
            var projectPath = Path.Combine(trackingPath, project);

            if (!Directory.Exists(projectPath))
            {
                return BadRequest($"Project {project} path don't exist");
            }

            var positions = TrackingService.GetCurrentPosition(projectPath, new List<string>());

            return Ok(positions);
        }

        [HttpGet("GetLatestPositions/{project}/json")]
        public IActionResult GetLatestPositionsAsJson(string project)
        {
            var dataPath = Config.Data?.FullName;
            var trackingPath = Path.Combine(dataPath, "tracking");
            var projectPath = Path.Combine(trackingPath, project);

            if (!Directory.Exists(projectPath))
            {
                return BadRequest($"Project {project} path don't exist");
            }

            var positions = TrackingService.GetLatestPosition(projectPath, new List<string>());
            var featureCollection = TrackingService.GetGeojson(positions);
            var contentResult = new ContentResult()
            {
                Content = JsonConvert.SerializeObject(featureCollection), ContentType = "application/json",
                StatusCode = 200
            };
            return contentResult;
        }

        [HttpGet("GetPositions/{project}/json/{start}/{end}")]
        public IActionResult GetPositionsAsJson(string project, DateTime start, DateTime end)
        {
            var dataPath = Config.Data?.FullName;
            var trackingPath = Path.Combine(dataPath, "tracking");
            var projectPath = Path.Combine(trackingPath, project);

            if (!Directory.Exists(projectPath))
            {
                return BadRequest($"Project {project} path don't exist");
            }

            var positions = TrackingService.GetPositions(projectPath, new List<string>(), start, end);
            var featureCollection = TrackingService.GetGeojson(positions);
            var contentResult = new ContentResult()
            {
                Content = JsonConvert.SerializeObject(featureCollection),
                ContentType = "application/json",
                StatusCode = 200
            };
            return contentResult;
        }
    }
}
