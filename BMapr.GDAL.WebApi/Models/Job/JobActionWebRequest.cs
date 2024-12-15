using BMapr.GDAL.WebApi.Services;
using System.Reflection.PortableExecutable;
using static BMapr.GDAL.WebApi.Services.WebRequestService;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobActionWebRequest : JobAction
    {
        public string Url { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public string Accept { get; set; } = string.Empty;

        public string BodyContent { get; set; } = string.Empty;

        public string BodyContentType { get; set; } = string.Empty;

        public int TimeOut { get; set; } = 30000;

        public bool AllowAutoRedirect { get; set; } = true;

        public string ReferenceName { get; set; } = string.Empty;

        public override Result Execute()
        {
            var result = new Result(){Succesfully = false };

            if (!Active)
            {
                result.Succesfully = true;
                result.AddMessage("Web request action is deactivated");
                return result;
            }

            var method = GetMethod(Method);

            if (method == MethodType.Unknown)
            {
                result.Messages.Add("Wrong request method");
                return result;
            }

            foreach (var header in Headers)
            {
                Headers[header.Key] = Replace(header.Value);
            }

            var  responseResult = WebRequestService.Request(new WebRequestParameter()
            {
                Url = Replace(Url),
                Method = method,
                Headers = Headers,
                BodyContent = Replace(BodyContent),
                BodyContentType = BodyContentType,
                Accept = Accept,
                TimeOut = TimeOut,
                AllowAutoRedirect = AllowAutoRedirect,
            });

            result.TakeoverEvents(responseResult);

            if (!string.IsNullOrEmpty(ReferenceName))
            {
                AddValue(ReferenceName, JobActionResultItemType.HtmlContent, System.Text.Encoding.UTF8.GetString(responseResult.Value));
                result.Succesfully = responseResult.Succesfully;
            }

            return result;
        }

        private MethodType GetMethod(string method)
        {
            switch (method.ToUpper())
            {
                case "GET":
                    return MethodType.Get;
                case "POST":
                    return MethodType.Post;
                case "PUT":
                    return MethodType.Put;
                case "DELETE":
                    return MethodType.Delete;
            }

            return MethodType.Unknown;
        }
    }
}
