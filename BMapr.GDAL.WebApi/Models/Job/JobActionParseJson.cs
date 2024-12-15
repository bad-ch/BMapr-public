using Newtonsoft.Json.Linq;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobActionParseJson : JobAction
    {
        public string Content { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string ReferenceName { get; set; } = string.Empty;

        public override Result Execute()
        {
            var result = new Result() { Succesfully = false };

            if (!Active)
            {
                result.Succesfully = true;
                result.AddMessage("Parse json action is deactivated");
                return result;
            }

            if (string.IsNullOrEmpty(Content) || string.IsNullOrEmpty(Path))
            {
                result.Succesfully = false;
                result.AddMessage("JSON content or path is empty");
                return result;
            }

            var modifiedContent = Replace(Content);

            JObject jsonObject = JObject.Parse(modifiedContent);

            var items = jsonObject.SelectTokens(Path).ToList();

            if (!string.IsNullOrEmpty(ReferenceName))
            {
                int counter = 0;

                foreach (JToken item in items)
                {
                    counter++;
                    AddValue(items.Count == 1 ? ReferenceName : $"{ReferenceName}{counter}", JobActionResultItemType.TextFile, item.ToString());
                }

                AddValue($"{ReferenceName}_Counter", JobActionResultItemType.TextFile, counter);
                result.Succesfully = counter > 0;
            }

            return result;
        }
    }
}
