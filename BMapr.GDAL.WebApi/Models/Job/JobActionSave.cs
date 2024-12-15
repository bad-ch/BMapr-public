using System.Text;

namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobActionSave : JobAction
    {
        public enum ActionType
        {
            SaveFile
        }

        public string FileName { get; set; }
        public string ReferenceName { get; set; }
        public ActionType Type { get; set; }
        public string Folder { get; set; }

        public override Result Execute()
        {
            var result = new Result() { Succesfully = false };

            if (!ResultItems.ContainsKey(ReferenceName))
            {
                result.Messages.Add($"Reference name <{ReferenceName}>unknown");
                return result;
            }

            var filePath = Path.Combine(string.IsNullOrEmpty(Folder) ? Config.DataProject(Project).FullName : Folder, Replace(FileName));
            var content = ResultItems[ReferenceName].Value;

            try
            {
                if (Type == ActionType.SaveFile)
                {
                    switch (content)
                    {
                        case byte[] byteContent:
                            File.WriteAllBytes(filePath, byteContent);
                            break;
                        case string stringContent:
                            File.WriteAllText(filePath, stringContent, Encoding.UTF8);
                            break;
                        default:
                            result.Messages.Add($"Type <{content.GetType().FullName}>not supported");
                            return result;
                    }
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
