using System.Text;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobActionLoad : JobAction
    {
        public enum ActionType
        {
            LoadFileText,
            LoadFileBytes
        }

        public string FileName { get; set; }
        public string ResultReferenceName { get; set; }
        public ActionType Type { get; set; }

        public override Result Execute()
        {
            var result = new Result() { Succesfully = false };

            var filePath = Path.Combine(Config.DataProject(Project).FullName, Replace(FileName));

            if (!File.Exists(filePath))
            {
                result.Messages.Add($"File not exists, project {Project}, FileName {FileName}");
                return result;
            }

            try
            {
                if (Type == ActionType.LoadFileText)
                {
                    var content = File.ReadAllText(filePath);
                    AddValue(ResultReferenceName, JobActionResultItemType.TextFile, content);
                }
                else if (Type == ActionType.LoadFileBytes)
                {
                    var content = File.ReadAllBytes(filePath);
                    AddValue(ResultReferenceName, JobActionResultItemType.File, content);
                }
                else
                {
                    result.Messages.Add($"Unknown action <{Type}>");
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Exceptions.Add(ex);
                return result;
            }

            result.Succesfully = true;
            return result;
        }
    }
}
