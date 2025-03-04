using BMapr.GDAL.WebApi.Authentication.Base;
using BMapr.GDAL.WebApi.Models;
using BMapr.GDAL.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using System.Net;
using System.Text;
using System.Web;

namespace BMapr.GDAL.WebApi.Controllers
{
    [ApiController]
    [Route("api/Proxy")]
    public class ProxyController : DefaultController
    {
        private readonly ILogger<ProxyController> _logger;
        private readonly IMemoryCache _cache;

        public ProxyController(ILogger<HomeController> logger, IConfiguration iConfig, IWebHostEnvironment environment, IMemoryCache cache) : base(iConfig, environment)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [BasicAuthorization]
        [HttpGet]
        [HttpPost]
        [HttpPut]
        [HttpDelete]
        public ActionResult Index()
        {
            if (!(Request.QueryString.HasValue && Request.QueryString.Value.Contains("url=")))
            {
                return BadRequest("url parameter is missing");
            }

            var url = HttpUtility.UrlDecode(Request.QueryString.Value.Replace("?url=", ""));
            
            Uri uri;
            Uri.TryCreate(url, UriKind.Absolute, out uri);

            var result = Proxy(uri, Request);

            if (result.Status != HttpStatusCode.OK)
            {
                return new ContentResult(){Content = Encoding.UTF8.GetString(result.Value), ContentType = result.ContentType, StatusCode = (int)result.Status};
            }

            // todo HttpContext.Response.Cookies
            // todo HttpContext.Response.Headers
            // HttpContext.Response.Headers.Add("test","test");

            return new FileContentResult(result.Value, result.ContentType);
        }

        private WebResult<byte[]> Proxy(Uri uri, HttpRequest requestSource)
        {
            var bodyContent = Request.GetRawBodyBytesAsync().Result;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(uri);
                request.Timeout = Convert.ToInt32(300000); //todo take from config
                request.Method = Request.Method;
                request.ContentType = requestSource.ContentType;

                //todo copy of headers necessary too

                if (requestSource.Cookies.Count > 0)
                {
                    request.CookieContainer = new CookieContainer();
                    var cookies = requestSource.Cookies;

                    foreach (var cookie in cookies)
                    {
                        request.CookieContainer.Add(new Cookie(cookie.Key, cookie.Value) { Domain = uri.Host });
                    }
                }

                request.Accept = requestSource.Headers["Accept"];
                request.UserAgent = requestSource.Headers["User-Agent"];

                if (request.Method == "POST")
                {
                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(bodyContent, 0, bodyContent.Length);
                    dataStream.Close();
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    byte[] content;

                    using (Stream responseStream = response.GetResponseStream())
                    {
                        content = StreamService.ReadToEnd(responseStream);
                    }

                    //todo headers and cookies

                    return new WebResult<byte[]>() { Value = content, Status = response.StatusCode, Encoding = response.ContentEncoding, ContentType = response.ContentType };
                }
            }
            catch (WebException wex)
            {
                Log.Error($"WebRequest fail: {uri}, {wex.Message}");

                if (wex.Response != null)
                {
                    byte[] content;
                    var response = ((HttpWebResponse)wex.Response);

                    using (Stream responseStream = response.GetResponseStream())
                    {
                        content = StreamService.ReadToEnd(responseStream);
                    }

                    return new WebResult<byte[]>() { Value = content, Status = response.StatusCode, Encoding = response.ContentEncoding, ContentType = response.ContentType, Exceptions = new List<Exception>() { wex } };
                }

                return new WebResult<byte[]>() { Value = Encoding.UTF8.GetBytes(wex.Message), Status = HttpStatusCode.InternalServerError, Encoding = "UTF8", ContentType = "text/plain", Exceptions = new List<Exception>() { wex } };
            }
            catch (Exception ex)
            {
                Log.Error($"WebRequest fail: {uri}, {ex.Message}");
                return new WebResult<byte[]>() { Value = Encoding.UTF8.GetBytes(ex.Message), Status = HttpStatusCode.InternalServerError, Encoding = "UTF8", ContentType = "text/plain", Exceptions = new List<Exception>() { ex } };
            }
        }
    }
}
