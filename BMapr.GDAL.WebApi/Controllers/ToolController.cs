using BMapr.GDAL.WebApi.Authentication.Base;
using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Text;


namespace BMapr.GDAL.WebApi.Controllers
{
    [ApiController]
    [Route("api/Tool")]
    public class ToolController : DefaultController
    {
        private readonly ILogger<ToolController> _logger;
        private readonly IMemoryCache _cache;

        public ToolController(ILogger<ToolController> logger, IConfiguration iConfig, IWebHostEnvironment environment, IMemoryCache cache) : base(iConfig, environment)
        {
            _logger = logger;
            _cache = cache;
        }

        [BasicAuthorization]
        [HttpPost("SavePost")]
        public IActionResult SavePost()
        {
            var bodyContent = Request.GetRawBodyBytesAsync().Result;
            var bodyContentType = Request.ContentType;

            if (bodyContent == null || bodyContent.Length == 0 || string.IsNullOrEmpty(bodyContentType))
            {
                return NoContent();
            }

            var dataPath = Config.Data?.FullName;
            var toolPath = Path.Combine(dataPath, "tool");
            var postSavePath = Path.Combine(toolPath, "postSave");

            if (!Directory.Exists(toolPath))
            {
                Directory.CreateDirectory(toolPath);

                if (!Directory.Exists(postSavePath))
                {
                    Directory.CreateDirectory(postSavePath);
                }
            }

            var ticks = DateTime.Now.Ticks;
            var guid = Guid.NewGuid();
            var postSaveFileName = $"{ticks}_{guid}.content";
            var postSaveFileExtensionName = $"{ticks}_{guid}.extension";

            var postSaveContentPath = Path.Combine(postSavePath, postSaveFileName);
            var postSaveExtensionPath = Path.Combine(postSavePath, postSaveFileExtensionName);

            try
            {
                System.IO.File.WriteAllBytes(postSaveContentPath, bodyContent);
                System.IO.File.WriteAllText(postSaveExtensionPath, bodyContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SavePost: {ex.Message}");
                return BadRequest(ex.Message);
            }

            return Ok(new {ticks,guid, postSaveFileName});
        }

        [BasicAuthorization]
        [HttpGet("GetPosts")]
        public IActionResult GetPosts()
        {
            //todo paging

            var dataPath = Config.Data?.FullName;
            var toolPath = Path.Combine(dataPath, "tool");
            var postSavePath = Path.Combine(toolPath, "postSave");

            if (!Directory.Exists(postSavePath))
            {
                return BadRequest(new {message = "no directory found"});
            }

            var postSaveDirectory = new DirectoryInfo(postSavePath);
            var files = new List<object>();

            foreach (var contentFile in postSaveDirectory.GetFiles("*.content"))
            {
                var content = System.IO.File.ReadAllBytes(contentFile.FullName);
                var mimeType = System.IO.File.ReadAllText(Path.Combine(postSavePath, contentFile.Name.Replace(".content",".extension")));

                if (mimeType.ToLower().Contains("text") || mimeType.ToLower().Contains("xml") || mimeType.ToLower().Contains("json") || mimeType.ToLower().Contains("text"))
                {
                    files.Add(new {fileName = contentFile.Name, length = content.Length, mimeType = mimeType, content = Encoding.UTF8.GetString(content) });
                    continue;
                }

                files.Add(new { fileName = contentFile.Name, length = content.Length, mimeType = mimeType, content = "" });
            }

            return Ok(files);
        }
    }
}
