using System.Net;
using System.Text;
using BMapr.GDAL.WebApi.Models;

namespace BMapr.GDAL.WebApi.Services
{
    public class WebRequestService
    {
        public enum MethodType
        {
            Get,
            Post,
            Put,
            Delete,
            Unknown
        }

        public static Result<byte[]> Request(WebRequestParameter webRequestParameter)
        {
            return Request(
                webRequestParameter.Url, 
                webRequestParameter.Method, 
                webRequestParameter.Headers,
                webRequestParameter.Accept, 
                webRequestParameter.BodyContent, 
                webRequestParameter.BodyContentType,
                webRequestParameter.TimeOut,
                webRequestParameter.AllowAutoRedirect
            );
        }

        public static Result<byte[]> Request(string url, MethodType method, Dictionary<string, string> headers, string accept, string bodyContent, string contentType, int timeOut, bool allowAutoRedirect)
        {
            var result = new Result<byte[]>(){Succesfully = false};
            var request = (HttpWebRequest)WebRequest.Create(new Uri(url));

            request.Method = method.ToString();
            request.AllowAutoRedirect = allowAutoRedirect;
            request.CookieContainer = new CookieContainer();
            request.Accept = accept;
            request.ContentType = contentType;
            request.UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 13_3_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36";

            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            request.Timeout = timeOut;

            if ((method == MethodType.Post || method == MethodType.Put) && !string.IsNullOrEmpty(bodyContent))
            {
                var bodyContentByteArray = Encoding.UTF8.GetBytes(bodyContent);
                var dataStream = request.GetRequestStream();
                dataStream.Write(bodyContentByteArray, 0, bodyContentByteArray.Length);
                dataStream.Close();
            }

            try
            {
                result.Value = GetBytesFromResponse(request.GetResponse());
                result.Succesfully = true;
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    result.Value = GetBytesFromResponse(wex.Response);
                }
                result.AddMessage($"web error request, {wex.Message}", wex, Result.LogLevel.Error);
            }
            catch (Exception ex)
            {
                result.AddMessage($"error request, {ex.Message}", ex, Result.LogLevel.Error);
            }

            return result;
        }

        public async Task<Result<byte[]>> RequestAsync(string url, MethodType method, Dictionary<string, string> headers, string accept, string bodyContent , string contentType, int timeOut, bool allowAutoRedirect)
        {
            return await Task.Run(async () => Request(url, method, headers, accept, bodyContent, contentType, timeOut, allowAutoRedirect));
        }

        private static byte[] GetBytesFromResponse(WebResponse response)
        {
            var webResponse = (HttpWebResponse)response;
            var ms = new MemoryStream();
            webResponse.GetResponseStream().CopyTo(ms);
            return ms.ToArray();
        }
    }
}
