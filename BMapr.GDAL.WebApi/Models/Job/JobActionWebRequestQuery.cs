using System.Collections;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using LiteDB;
using Newtonsoft.Json;
using Quartz;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobActionWebRequestQuery : JobActionWebRequest
    {
        public string ByAttribute { get; set; }

        public string ByAttributeValue { get; set; }

        public string XPath { get; set; }

        public string ReferenceNameQuery { get; set; }

        public override Result Execute()
        {
            var result = base.Execute();

            if (!result.Succesfully)
            {
                return result;
            }

            var resultWr = new Result() {Succesfully = false};
            resultWr.TakeoverEvents(result);

            if (!ResultItems.ContainsKey(ReferenceName))
            {
                resultWr.AddMessage($"ReferenceName <{ReferenceName}> not found");
                return resultWr;
            }

            string content;

            if (ResultItems[ReferenceName].Value is byte[])
            {
                content = System.Text.Encoding.UTF8.GetString((byte[])ResultItems[ReferenceName].Value);
            }
            else if (ResultItems[ReferenceName].Value is string)
            {
                content = (string) ResultItems[ReferenceName].Value;
            }
            else
            {
                resultWr.AddMessage("wrong type of data from web request");
                return resultWr;
            }

            var htmlDocument = new HtmlAgilityPack.HtmlDocument();
            htmlDocument.LoadHtml(content);
            List<HtmlPart> queryNodes;

            if (!string.IsNullOrEmpty(ByAttribute))
            {
                queryNodes = htmlDocument.DocumentNode.SelectNodes($"//descendant::*[contains(@{ByAttribute}, '{ByAttributeValue}')]").Select(x =>new HtmlPart() {
                    OuterHtml = x.OuterHtml,
                    XPath = x.XPath,
                    Line = x.Line,
                    LinePosition = x.LinePosition,
                }).ToList();
            }
            else
            {
                queryNodes  = htmlDocument.DocumentNode.SelectNodes(XPath).Select(x => new HtmlPart()
                {
                    OuterHtml = x.OuterHtml,
                    XPath = x.XPath,
                    Line = x.Line,
                    LinePosition = x.LinePosition,
                }).ToList();
            }

            if (queryNodes == null)
            {
                resultWr.AddMessage($"no element found: //descendant::*[contains(@{ByAttribute}, '{ByAttributeValue}')]");
                return resultWr;
            }

            AddValue(ReferenceNameQuery, JobActionResultItemType.Json, JsonConvert.SerializeObject(queryNodes, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize
                    }
                )
            );

            return resultWr;
        }
    }
}
