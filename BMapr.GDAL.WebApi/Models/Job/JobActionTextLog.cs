namespace BMapr.GDAL.WebApi.Models.Job
{
    public class JobActionTextLog : JobAction
    {
        public string LocalPath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool Append { get; set; } = true;

        public override Result Execute()
        {
            var path = Path.Combine(Config.DataProject(Project).FullName, LocalPath);
            var result = new Result() { Succesfully = false };
            var filepath = Path.Combine(path, FileName);

            if (!Active)
            {
                result.Succesfully = true;
                result.AddMessage("TextLog action is deactivated");
                return result;
            }

            if (!Directory.Exists(path))
            {
                result.Messages.Add($"Path don't exist {path}");
                return result;
            }

            // todo apply placeholder engine to Filename, LocalPath

            try
            {
                var content = Replace(Content);

                if (Append)
                {
                    File.AppendAllText(filepath, content);
                }
                else
                {
                    File.WriteAllText(filepath, content);
                }
            }
            catch (Exception ex)
            {
                result.Exceptions.Add(ex);
                result.Messages.Add($"Error text log action {ex.Message}");

                return result;
            }

            result.Succesfully = true;
            return result;
        }
    }
}
